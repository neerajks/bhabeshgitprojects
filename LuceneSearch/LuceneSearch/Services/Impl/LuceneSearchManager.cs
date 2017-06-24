using LuceneSearch.Services.Inte;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuceneSearch.DataModel;
using Lucene.Net.Store;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using System.Diagnostics;

namespace LuceneSearch.Services.Impl
{
    public class LuceneSearchManager : ISearchManager
    {

        //TODO: The Index writer and the searcher can be segregated into diff classes.
        //StandardAnalyzer _analyser = null;
        //FSDirectory _indexDirectory = null;
        //IndexWriter _indexWriter = null;

        //IndexSearcher _indexSearcher = null;


        public LuceneSearchManager()
        {

        }
        SearchContext _searchContext = null;

        Directory _indexDirectory;

        public bool BuildIndex(SearchContext context, IList<DocumentData> documentDataList)
        {
            _searchContext = context;
            try
            {
                if (System.IO.Directory.Exists(context.IndexPath))
                {
                    System.IO.Directory.Delete(context.IndexPath, true);
                }

                using (var indexDirectory = FSDirectory.Open(context.IndexPath))
                using (var analyser = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30, new HashSet<string> { "_" }))
                using (var indexWriter = new IndexWriter(indexDirectory, analyser, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    var indexSearcher = new IndexSearcher(indexDirectory, true);

                    foreach (var doc in documentDataList)
                    {
                        Document document = new Document();
                        Field nameField = new Field("name", doc.FileName, Field.Store.YES, Field.Index.ANALYZED);
                        Field pathField = new Field("path", doc.FilePath, Field.Store.YES, Field.Index.ANALYZED);
                        document.Add(nameField);
                        document.Add(pathField);
                        indexWriter.AddDocument(document);
                        Trace.WriteLine(string.Format("Added {0} to Index", doc.FilePath));
                        //System.IO.Path.GetDirectoryName(doc.FilePath), doc.FileName));
                    }

                    indexWriter.Optimize();
                    indexWriter.Flush(true, true, true);
                    indexWriter.Commit();
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public IList<DocumentData> Search(string searchString)
        {
            IList<DocumentData> documentDataList = new List<DocumentData>();
            try
            {
                using (var indexDirectory = FSDirectory.Open(_searchContext.IndexPath))
                using (var indexSearcher = new IndexSearcher(indexDirectory, true))
                using (var analyser = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30, new HashSet<string> { "_" }))
                {
                    QueryParser queryParser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "name", analyser);
                    var query = queryParser.Parse(searchString);
                    var collector = TopScoreDocCollector.Create(500, true);
                    indexSearcher.Search(query, collector);
                    var topDocs = collector.TopDocs();
                    foreach (var item in topDocs.ScoreDocs)
                    {
                        var luDoc = indexSearcher.Doc(item.Doc);
                        documentDataList.Add(new DocumentData { FileName = luDoc.Get("name"), FilePath = luDoc.Get("path") });
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return documentDataList;
        }
    }
}

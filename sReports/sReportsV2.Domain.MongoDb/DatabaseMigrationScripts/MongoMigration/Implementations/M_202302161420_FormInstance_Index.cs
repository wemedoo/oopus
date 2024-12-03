using MongoDB.Driver;
using sReportsV2.Common.Constants;
using sReportsV2.Domain.Entities.FormInstance;
using sReportsV2.Domain.Mongo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sReportsV2.Domain.DatabaseMigrationScripts
{
    public class M_202302161420_FormInstance_Index : MongoMigration
    {
        private IMongoCollection<FormInstance> Collection;
        public override int Version => 3;

        public M_202302161420_FormInstance_Index()
        {
            Collection = MongoDBInstance.Instance.GetDatabase().GetCollection<FormInstance>(MongoCollectionNames.FormInstance);
        }

        protected override void Up()
        {
            IndexKeysDefinition<FormInstance> key = Builders<FormInstance>.IndexKeys
                .Ascending("IsDeleted")
                .Ascending("ThesaurusId")
                .Ascending("Version._id")
                ;
            Collection.Indexes.CreateOne(new CreateIndexModel<FormInstance>(key, new CreateIndexOptions() { Name = "By_FormDefinition"}));
        }

        protected override void Down()
        {
            Collection.Indexes.DropOne("By_FormDefinition");
        }
    }
}

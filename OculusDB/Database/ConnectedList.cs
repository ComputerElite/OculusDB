using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OculusDB.MongoDB;

namespace OculusDB.Database
{
    public class ConnectedList : DLCLists
    {
        public List<DBApplication> applications { get; set; } = new List<DBApplication>();
        public List<DBVersion> versions { get; set; } = new List<DBVersion>();
        public List<DBAchievement> achievements { get; set; } = new List<DBAchievement>();

        public void PopulateAll(PopulationContext context)
        {
            base.PopulateAll(context);
            applications.ForEach(x => x.PopulateSelf(context));
            versions.ForEach(x => x.PopulateSelf(context));
            achievements.ForEach(x => x.PopulateSelf(context));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerBIServiceBackup.Models
{
    public class GroupReport
    {
        private string groupId;
        private string reportId;
        private string groupName;

        public GroupReport(string groupId, string reportId, string groupName)
        {
            this.GroupId = groupId;
            this.ReportId = reportId;
            this.groupName = groupName;
        }

        public string GroupId { get => groupId; set => groupId = value; }
        public string ReportId { get => reportId; set => reportId = value; }
        public string GroupName { get => groupName; set => groupName = value; }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}

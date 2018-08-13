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

        public GroupReport(string groupId, string reportId)
        {
            this.GroupId = groupId;
            this.ReportId = reportId;
        }

        public string GroupId { get => groupId; set => groupId = value; }
        public string ReportId { get => reportId; set => reportId = value; }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}

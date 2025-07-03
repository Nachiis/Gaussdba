using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gaussdb
{
    [Table("yangyw_design_users")]
    public class yangyw_design_users
    {
        public string? yyw_username { get; set; }
        public string? yyw_password { get; set; }
        public int? yyw_role { get; set; }
    }
}

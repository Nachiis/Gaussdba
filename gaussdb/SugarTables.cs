using SqlSugar;
using System;
using System.Linq;
using System.Text;

namespace Gaussdb
{
    ///<summary>  
    ///  
    ///</summary>  
    public partial class yangyw_design_users
    {
        public yangyw_design_users()
        {
        }

        /// <summary>  
        /// Desc:  
        /// Default:  
        /// Nullable:False  
        /// </summary>     
        [SugarColumn(IsPrimaryKey = true)]
        public string yyw_username { get; set; }

        /// <summary>  
        /// Desc:  
        /// Default:  
        /// Nullable:False  
        /// </summary>             
        public string yyw_password { get; set; }

        /// <summary>  
        /// Desc:  
        /// Default:0  
        /// Nullable:False  
        /// </summary>             
        public short yyw_role { get; set; }
    }
}

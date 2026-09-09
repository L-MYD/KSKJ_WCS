using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WCS_Models.WCSModel.LogModel
{
    [Table("logs")]
    public class LogModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]   // 添加此行
        public int Id { get; set; }

        [Column("usertype")]
        public string UserType { get; set; }

        [Column("logtype")]
        public string LogType { get; set; }

        [Column("level")]
        public string Level { get; set; }

        [Column("message")]
        public string Message { get; set; }

        [Column("module")]
        public string Module { get; set; }

        [Column("operation")]
        public string Operation { get; set; }

        [Column("details")]
        public string Details { get; set; }

        [Column("userid")]
        public string UserId { get; set; }

        [Column("ipaddress")]
        public string IpAddress { get; set; }

        [Column("createtime")]   // 注意：全小写
        public DateTime CreateTime { get; set; }

        [Column("isarchived")]
        public string IsArchived { get; set; } = "False";
    }
}
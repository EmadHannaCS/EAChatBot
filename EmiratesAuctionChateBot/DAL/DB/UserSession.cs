using System;
using System.Collections.Generic;

namespace DAL.DB
{
    public partial class UserSession
    {
        public int Id { get; set; }
        public string UserPhone { get; set; }
        public string LastSessionId { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

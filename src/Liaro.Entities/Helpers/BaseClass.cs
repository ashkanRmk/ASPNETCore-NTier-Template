using System;

namespace Liaro.Entities.Helpers
{
    public class BaseClass
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }
    }
}
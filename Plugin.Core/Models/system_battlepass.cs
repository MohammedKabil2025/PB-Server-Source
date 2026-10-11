using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Game.Data.Models
{
    public class BattlePassReward
    {
        public int Id { get; set; }              // Level
        public bool Active { get; set; }         // Active season
        public int Status { get; set; }          // Season status
        public string Name { get; set; }         // Season name
        public int Days { get; set; }            // Duration in days
        public long StartDate { get; set; }      // Unix timestamp
        public long EndDate { get; set; }        // Unix timestamp
        public int GoodId { get; set; }          // Free reward
        public int GoodIdPA { get; set; }        // Premium A reward
        public int GoodIdPB { get; set; }        // Premium B reward
        public int PointReq { get; set; }        // Required EXP
    }
}

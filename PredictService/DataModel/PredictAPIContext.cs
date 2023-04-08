using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PredictSrevice;

namespace PredictAPI.Data
{
    public class PredictAPIContext : DbContext
    {
        public PredictAPIContext (DbContextOptions<PredictAPIContext> options)
            : base(options)
        {
        }

        public DbSet<PredictSrevice.retrain> retrain { get; set; } = default!;
    }
}

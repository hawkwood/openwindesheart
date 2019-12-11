﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WindesHeartApp.Models;

namespace WindesHeartApp.Data.Interfaces
{
    public interface ISleepRepository
    {
        Task<IEnumerable<Sleep>> GetAllAsync();
        Task<bool> AddAsync(Sleep sleep);
        Task<bool> AddRangeAsync(List<Sleep> sleep);
        void RemoveAll();
        Task<IEnumerable<Sleep>> SleepByQueryAsync(Func<Sleep, bool> predicate);
        void SaveChangesAsync();
    }
}
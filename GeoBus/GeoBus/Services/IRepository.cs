using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeoBus.Services {
    internal interface IRepository<T> {
        Task<string> Create(T _T);
        IEnumerable<T> ReadAll();
    }
}
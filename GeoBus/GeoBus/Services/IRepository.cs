using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeoBus.Services {
    internal interface IRepository<T> {
        Task<string> Create(T _T);
        Task<T> Read(string key);
        Task<T> Read(Func<T, bool> filter);
        Task<IEnumerable<T>> ReadAll();
        Task<IEnumerable<T>> ReadAll(Func<T, bool> filter);
        Task Update(T value, string key);
        Task Update(T value, Func<T, bool> func);
        Task Delete(string key);
        Task Delete(Func<T, bool> func);
    }
}
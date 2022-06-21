using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Firebase.Database;
using Firebase.Database.Query;

using Newtonsoft.Json;

using static GeoBus.Services.Keys;

namespace GeoBus.Services {
    public class FirebaseDatabaseService<T> : IRepository<T> where T : class {

        public FirebaseClient _client = new FirebaseClient(BaseUrl, new FirebaseOptions() { AuthTokenAsyncFactory = () => Task.FromResult(baseSecret) });

        private string table;
        public FirebaseDatabaseService(string table) {
            this.table = table;
        }

        public async Task<string> Create(T _T) {
            var result = await _client.Child(table).PostAsync(JsonConvert.SerializeObject(_T));
            return result.Key;
        }

        public async Task<T> Read(Func<T, bool> filter) {
            var values = (await _client.Child(table).OnceAsync<T>()).Select(u => u.Object);

            return values.FirstOrDefault(filter);
        }
        public async Task<T> Read(string key) {
            var values = await _client.Child(table).Child(key).OnceSingleAsync<T>();
            return values;
        }

        public async Task<IEnumerable<T>> ReadAll() {
            var values = await _client.Child(table).OrderByKey().OnceAsync<T>();
            return values.Select(u => u.Object);
        }

        public async Task<IEnumerable<T>> ReadAll(Func<T, bool> filter) {
            var values = await _client.Child(table).OrderByKey().OnceAsync<T>();
            return values.Select(u => u.Object);
        }

        public async Task Update(T value, Func<T, bool> filter) {
            var values = (await _client.Child(table).OnceAsync<T>()).ToList().FirstOrDefault(x => filter(x.Object));
            await _client.Child(table).Child(values.Key).PutAsync(value);
        }
        public async Task Update(T value, string key) {
            await _client.Child(table).Child(key).PutAsync(value);
        }

        public async Task Delete(Func<T, bool> filter) {
            var values = (await _client.Child(table).OnceAsync<T>()).ToList().FirstOrDefault(x => filter(x.Object));
            await _client.Child(table).Child(values.Key).DeleteAsync();
        }
        public async Task Delete(string key) {
            await _client.Child(table).Child(key).DeleteAsync();
        }
        public async Task<string> GetKey(Func<T, bool> filter) {
            var values = (await _client.Child(table).OnceAsync<T>()).ToList().FirstOrDefault(x => filter(x.Object));
            return values.Key;
        }
    }
}
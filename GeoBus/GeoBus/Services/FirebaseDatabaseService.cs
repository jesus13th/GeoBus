using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Firebase.Database;

using Newtonsoft.Json;

using static GeoBus.Services.Keys;

namespace GeoBus.Services {
    public class FirebaseDatabaseService<T> : IRepository<T> where T : class {
        public FirebaseClient _client = new FirebaseClient(BaseUrl, new FirebaseOptions() { AuthTokenAsyncFactory = () => Task.FromResult(baseSecret) });
        private IReadOnlyCollection<FirebaseObject<T>> data;

        private string table;
        public FirebaseDatabaseService(string table) {
            this.table = table;
            GetDatabase();
        }
        private async void GetDatabase() => data = await _client.Child(table).OnceAsync<T>();

        public async Task<string> Create(T _T) {
            var result = await _client.Child(table).PostAsync(JsonConvert.SerializeObject(_T));
            return result.Key;
        }

        public IEnumerable<T> ReadAll() {
            var values = data;
            return values.Select(u => u.Object);
        }
    }
}
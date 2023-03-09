using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Data.SqlClient;


namespace ObserverWoodDeal
{
    internal class Program
    {
        //Строка подключения локальной БД
        private static string connectionString = @"Data Source=(localdb)\LesegaisLoc;Initial Catalog=ReportWoodDealDb;Integrated Security=True";
        
        static async Task Main(string[] args)
        {
            var statetimer = new System.Threading.Timer(LoopRequestAsync, null, 0, 600000);
            Console.ReadKey();
        }

        private static async void LoopRequestAsync(object stateInfo)
        {
            Console.WriteLine("Получение количества записей");
            int countDeal = await GetIntAsync();
            Console.WriteLine("Записей: "+ countDeal);
            int countRequest = 0;
            int sizeRequest = countDeal / 4;
            while (countDeal > 0) 
            {
                Console.WriteLine("\n________________");
                //Console.Clear();
                Console.WriteLine("Цикл "+(countRequest+1));
                Console.WriteLine("Количество записей " + sizeRequest + "\n");
                Console.WriteLine("Отправка запроса на получение записей...");
                WoodDeal[] dataRequest = await GetRequest(sizeRequest, countRequest);
                Console.WriteLine("Полученное количество записей: " + dataRequest.Count() + "\n");
                Console.WriteLine("Сохранение данных...");
                await SaveDataAsync(dataRequest);
                countRequest++;
                countDeal -= sizeRequest;
                Console.WriteLine("________________\n");
            }
        }
        
        //Запрос на получение ВСЕГО количества сделок
        private static async Task<int> GetIntAsync() 
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.lesegais.ru/open-area/graphql");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36 OPR/95.0.0.0");
            var content = new StringContent("{\"query\":\"query SearchReportWoodDealCount(^$size: Int^!, ^$number: Int^!, ^$filter: Filter, ^$orders: [Order^!]) {\\n  searchReportWoodDeal(filter: ^$filter, pageable: {number: ^$number, size: ^$size}, orders: ^$orders) {\\n    total\\n    number\\n    size\\n    overallBuyerVolume\\n    overallSellerVolume\\n    __typename\\n  }\\n}\\n\",\"variables\":{\"size\":20,\"number\":0,\"filter\":null},\"operationName\":\"SearchReportWoodDealCount\"}", null, "application/json");
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var resContent = await response.Content.ReadAsStringAsync();
            return Int32.Parse(resContent.Substring(41, resContent.IndexOf(',') - 41));
        }

        //Запрос на получение сделок, в нём есть параметры с которыми можно эксперементировать
        public static async Task<WoodDeal[]> GetRequest(int sizeRequest, int countRequest)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://www.lesegais.ru/open-area/graphql");
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36 OPR/95.0.0.0");
            request.Content = new StringContent("{\"query\":\"query SearchReportWoodDeal(^$size: Int^!, ^$number: Int^!, ^$filter: Filter, ^$orders: [Order^!]) {\\n  searchReportWoodDeal(filter: ^$filter, pageable: {number: ^$number, size: ^$size}, orders: ^$orders) {\\n    content {\\n      sellerName\\n      sellerInn\\n      buyerName\\n      buyerInn\\n      woodVolumeBuyer\\n      woodVolumeSeller\\n      dealDate\\n      dealNumber\\n      __typename\\n    }\\n    __typename\\n  }\\n}\\n\",\"variables\":{\"size\":"+sizeRequest+",\"number\":"+countRequest+",\"filter\":null,\"orders\":null},\"operationName\":\"SearchReportWoodDeal\"}", null, "text/plain");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var resContent = await response.Content.ReadAsStringAsync();
            resContent = resContent.Substring(43, resContent.IndexOf(']') - 42);
            return JsonSerializer.Deserialize<WoodDeal[]>(resContent);
        }

        public static async Task SaveDataAsync(WoodDeal[] woodDeals)
        {
            string query = @"IF EXISTS(SELECT * FROM Deal WHERE dealNumber = @dealNumber)
                        UPDATE Deal 
                        SET sellerName = @sellerName,
                            sellerInn = @sellerInn,
                            buyerName = @buyerName,
                            buyerInn = @buyerInn,
                            woodVolumeBuyer = @woodVolumeBuyer,
                            woodVolumeSeller = @woodVolumeSeller,
                            dealDate = @dealDate
                        WHERE dealNumber = @dealNumber
                    ELSE
                        INSERT INTO Deal (dealNumber, sellerName, sellerInn, buyerName, buyerInn, woodVolumeBuyer, woodVolumeSeller, dealDate) VALUES (@dealNumber, @sellerName, @sellerInn, @buyerName, @buyerInn, @woodVolumeBuyer, @woodVolumeSeller, @dealDate)";
            int countAddDeal = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    Console.WriteLine("Подключение...");
                    connection.Open();
                    Console.WriteLine("Запись данных...");
                    foreach (var woodD in woodDeals)
                    {
                        //В графе очень кривые записи с датой заключение в 903 году. Я не знаю как такое можно исправить
                        SqlCommand command = new SqlCommand(query, connection);
                        command.Parameters.Add(new SqlParameter("@dealNumber", woodD.dealNumber));
                        if (woodD.sellerName != null) command.Parameters.Add(new SqlParameter("@sellerName", woodD.sellerName));
                        else command.Parameters.Add(new SqlParameter("@sellerName", ' '));
                        if (woodD.sellerInn != null) command.Parameters.Add(new SqlParameter("@sellerInn", woodD.sellerInn));
                        else command.Parameters.Add(new SqlParameter("@sellerInn", ' '));
                        if (woodD.buyerName != null) command.Parameters.Add(new SqlParameter("@buyerName", woodD.buyerName));
                        else command.Parameters.Add(new SqlParameter("@buyerName", ' '));
                        if (woodD.buyerInn != null) command.Parameters.Add(new SqlParameter("@buyerInn", woodD.buyerInn));
                        else command.Parameters.Add(new SqlParameter("@buyerInn", ' '));
                        command.Parameters.Add(new SqlParameter("@woodVolumeBuyer", woodD.woodVolumeBuyer));
                        command.Parameters.Add(new SqlParameter("@woodVolumeSeller", woodD.woodVolumeSeller));
                        if(woodD.dealDate != null) command.Parameters.Add(new SqlParameter("@dealDate", woodD.dealDate));
                        else command.Parameters.Add(new SqlParameter("@dealDate", new DateTime(1753,1,1)));
                        await command.ExecuteNonQueryAsync();
                        countAddDeal++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    Console.WriteLine("Внесенно записей: "+countAddDeal+" из "+woodDeals.Count());
                    connection.Close();
                }
            }
        }
    }
}


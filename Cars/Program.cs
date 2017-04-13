using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cars
{
    class Program
    {
        static void Main(string[] args)
        {
            var cars = ProcessCars("fuel.csv");
            var manufacturers = ProcessManufacturers("manufacturers.csv");

            var query_join =
                from car in cars
                join manufacturer in manufacturers 
                    on new { car.Manufacturer, car.Year } 
                        equals new { Manufacturer = manufacturer.Name, manufacturer.Year }
                orderby car.Combined descending, car.Name ascending
                select new
                {
                    manufacturer.Headquarters,
                    car.Name,
                    car.Combined
                };

            var query_join2 =
                cars.Join(manufacturers,
                            c => new { c.Manufacturer, c.Year },
                            m => new { Manufacturer = m.Name, m.Year },
                            (c, m) => new
                            {
                               Car = c,
                               Manufacturer = m
                            })
                     .OrderByDescending(c => c.Car.Combined)
                     .ThenBy(c => c.Car.Name)
                     .Select(c => new {
                         c.Manufacturer.Headquarters,
                         c.Car.Name,
                         c.Car.Combined
                     });

            //foreach (var car in query_join2.Take(10))
            //{
            //    Console.WriteLine($"{car.Headquarters,-12} {car.Name,-20} : {car.Combined}");
            //}

            var query_group =
                from car in cars
                group car by car.Manufacturer.ToUpper() into manufacturer
                orderby manufacturer.Key
                select manufacturer;

            var query_group2 =
                cars.GroupBy(c => c.Manufacturer.ToUpper())
                    .OrderBy(g => g.Key);

            //foreach (var group in query_group2)
            //{
            //    Console.WriteLine(group.Key);
            //    foreach (var car in group.OrderByDescending(c => c.Combined).Take(2))
            //    {
            //        Console.WriteLine($"\t{car.Name} : {car.Combined}");
            //    }
            //}

            var q_groupjoin =
                from manufacturer in manufacturers
                join car in cars on manufacturer.Name equals car.Manufacturer
                    into carGroup
                orderby manufacturer.Name
                select new
                {
                    Manufacturer = manufacturer,
                    Cars = carGroup
                };

            var q_groupjoin2 =
                manufacturers.GroupJoin(cars, m => m.Name, c => c.Manufacturer, 
                                        (m, g) => new
                                        {
                                            Manufacturer = m,
                                            Cars = g
                                        })
                .OrderBy(m => m.Manufacturer.Name);

            //foreach (var group in q_groupjoin)
            //{
            //    Console.WriteLine($"{group.Manufacturer.Name}: {group.Manufacturer.Headquarters}");
            //    foreach (var car in group.Cars.OrderByDescending(c => c.Combined).Take(2))
            //    {
            //        Console.WriteLine($"\t{car.Name} : {car.Combined}");
            //    }
            //}

            var query_country =
                from car in cars
                join manufacturer in manufacturers
                    on car.Manufacturer equals manufacturer.Name
                group new { Car = car, Manufacturer = manufacturer } by manufacturer.Headquarters into country
                orderby country.Key
                select country;

            //foreach (var country in query_country)
            //{
            //    Console.WriteLine(country.Key);
            //    foreach (var car in country.OrderByDescending(c => c.Car.Combined).Take(3))
            //    {
            //        Console.WriteLine($"\t{car.Manufacturer.Name} {car.Car.Name} : {car.Car.Combined}");
            //    }
            //}

            var query_country_alternative =
                from manufacturer in manufacturers
                join car in cars on manufacturer.Name equals car.Manufacturer
                    into carGroup
                orderby manufacturer.Name
                select new
                {
                    Manufacturer = manufacturer,
                    Cars = carGroup
                } into result
                group result by result.Manufacturer.Headquarters into country
                orderby country.Key
                select country;
            // here then we need SelectMany for flattening

            //foreach (var country in query_country_alternative)
            //{
            //    Console.WriteLine(country.Key);
            //    foreach (var car in country.SelectMany(g => g.Cars).OrderByDescending(c => c.Combined).Take(3))
            //    {
            //        Console.WriteLine($"\t{car.Manufacturer} {car.Name} : {car.Combined}");
            //    }
            //}

            var query_aggregation =
                from car in cars
                group car by car.Manufacturer into carGroup
                select new
                {
                    Name = carGroup.Key,
                    Max = carGroup.Max(c => c.Combined),
                    Min = carGroup.Min(c => c.Combined),
                    Avg = carGroup.Average(c => c.Combined)
                } into result
                orderby result.Max descending
                select result;

            var query_aggregation2 =
                cars.GroupBy(c => c.Manufacturer)
                    .Select(g =>
                    {
                        var results = g.Aggregate(new CarStatistics(),
                                            (acc, c) => acc.Accumulate(c),
                                            acc => acc.Compute());
                        return new
                        {
                            Name = g.Key,
                            Avg = results.Avg,
                            Max = results.Max,
                            Min = results.Min
                        };
                    })
                    .OrderByDescending(r => r.Max);

            foreach (var result in query_aggregation)
            {
                Console.WriteLine(result.Name);
                Console.WriteLine($"\t Max: {result.Max}");
                Console.WriteLine($"\t Min: {result.Min}");
                Console.WriteLine($"\t Avg: {result.Avg,5:N2}");
            }
        }

        private static List<Manufacturer> ProcessManufacturers(string path)
        {
            var query =
                File.ReadAllLines(path)
                    .Where(l => l.Length > 1)
                    .Select(l =>
                    {
                        var columns = l.Split(',');
                        return new Manufacturer
                        {
                            Name = columns[0],
                            Headquarters = columns[1],
                            Year = int.Parse(columns[2])
                        };
                    });
            return query.ToList();
        }

        private static List<Car> ProcessCars(string path)
        {
            var query =
                    File.ReadAllLines(path)
                        .Skip(1)
                        .Where(line => line.Length > 1)
                        .ToCar();

            var query2 =
                from line in File.ReadAllLines(path).Skip(1)
                where line.Length > 1
                select Car.ParseFromCsv(line);

            return query.ToList();
        }


    }



    public class CarStatistics
    {
        public CarStatistics()
        {
            Max = Int32.MinValue;
            Min = Int32.MaxValue;
        }
        public CarStatistics Accumulate(Car car)
        {
            Count += 1;
            Total += car.Combined;
            Max = Math.Max(Max, car.Combined);
            Min = Math.Min(Min, car.Combined);

            return this;
        }

        public CarStatistics Compute()
        {
            Avg = (double)Total / Count;
            return this;
        }

        public int Max { get; set; }
        public int Min { get; set; }
        public int Total { get; set; }
        public int Count { get; set; }
        public double Avg { get; set; }
    }


    public static class CarExtensions
    {
        private static NumberFormatInfo _nfi = new CultureInfo("en-US", false).NumberFormat;

        public static IEnumerable<Car> ToCar(this IEnumerable<string> source)
        {
            foreach (var line in source)
            {
                var columns = line.Split(',');

                yield return new Car
                {
                    Year = int.Parse(columns[0]),
                    Manufacturer = columns[1],
                    Name = columns[2],
                    Displacement = double.Parse(columns[3], _nfi),
                    Cylinders = int.Parse(columns[4]),
                    City = int.Parse(columns[5]),
                    Highway = int.Parse(columns[6]),
                    Combined = int.Parse(columns[7])
                };
            }
        }
    }
}

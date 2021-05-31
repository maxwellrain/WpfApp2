using System;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using ClosedXML.Excel;
using System.Collections.Generic;
using System.Linq;
using LiveCharts.Defaults;

namespace Wpf.CartesianChart.PointShapeLine
{
    public partial class ReadExceleAndPrintGraphic : UserControl
    {
        List<double> Times = new List<double>();
        List<double> LeftEye = new List<double>();
        List<double> RightEye = new List<double>();
        int RowsCount => Times.Count;

        public SeriesCollection SeriesBase { get; set; }
        public SeriesCollection SeriesEyesDelta { get; set; }
        public SeriesCollection SeriesBoxPlot { get; set; }
        public string[] XLable { get; private set; }
        public Func<double, string> YFormatter { get; set; }


        public ReadExceleAndPrintGraphic()
        {
            InitializeComponent();

            ReadEyesDataFromExcele("ExampleEyesData.xlsx");

            SeriesBase = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Right Eye",
                    Values = new ChartValues<double> (RightEye)
                },
                new LineSeries
                {
                    Title = "Left Eye",
                    Values = new ChartValues<double> (LeftEye),
                    PointGeometry = null
                }
            };

            List<double> Delta = new List<double>();
            for(int i = 0; i < RowsCount; i++)
            {
                Delta.Add(LeftEye[i] - RightEye[i]);
            }

            SeriesEyesDelta = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Delta",
                    Values = new ChartValues<double> (Delta)
                }
            };

            XLable = Times.ConvertAll<string>(delegate (double d) { return d.ToString(); }).ToArray();
            YFormatter = value => value.ToString();

            SeriesBoxPlot = new SeriesCollection
            {
                new CandleSeries
                {
                    Values = new ChartValues<OhlcPoint>
                    {
                        new OhlcPoint(Quantile(1, LeftEye, GetProbability(LeftEye)), Max(LeftEye), Min(LeftEye), Quantile(3, LeftEye, GetProbability(LeftEye))),
                        new OhlcPoint(32, 35, 30, 32),
                    }
                }
            };


            DataContext = this;
        }

        void ReadEyesDataFromExcele(string xlsxpath)
        {
            //TODO обработать отсутствие файла
            // Открываем книгу
            var workbook = new XLWorkbook(xlsxpath);
            // Берем в ней первый лист
            IXLWorksheet worksheet = workbook.Worksheets.First();
            int TimeColumnNum = 1;
            int LeftEyeColumnNum = 2;
            int RightEyeColumnNum = 3;

            //Получаем все непустыне строки в таблице
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // И пропускаем первую строку-заголовок
            foreach (var row in rows)
            {
                Times.Add(row.Cell(TimeColumnNum).GetDouble());
                LeftEye.Add(row.Cell(LeftEyeColumnNum).GetDouble());
                RightEye.Add(row.Cell(RightEyeColumnNum).GetDouble());
            }
        }

        static public double Quantile(double quantil, double[] arr_num, double[] arr_prob)
        {
            double result = 0;
            for (int i = 0; i < arr_num.Length; i++)
            {
                //Todo провыерка на выход за массив?
                if (arr_prob[i] <= quantil)
                {
                    result = arr_num[i];
                    continue;
                }
            }
            return result;
        }

        List<double> GetProbability(List<double> list)
        {

            return new List<double>();
        }




        //private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        //{
        //    var metrics = ReadEyesDataFromExcele("ExampleEyesData.xlsx");
        //    MetricsDataGrid.ItemsSource = metrics;
        //}
    }

}
/*
 * public class Metric
    {
        public double Time { get; set; }
        public double LeftEye { get; set; }
        public double RigthEye { get; set; }
    }
    static IEnumerable<Metric> EnumerateMetrics(string xlsxpath)
    {
        //TODO обработать отсутствие файла
        // Открываем книгу
        var workbook = new XLWorkbook(xlsxpath);

        // Берем в ней первый лист
        IXLWorksheet worksheet = workbook.Worksheets.First();
        var totalRows = worksheet.RowsUsed().Count();
        int LeftEyeColumnNum = 2;
        int RightEyeColumnNum = 3;
        // Перебираем диапазон нужных строк
        for (int row = 2; row <= totalRows; ++row)
        {
            // По каждой строке формируем объект
            var metric = new Metric
            {
                //TODO добавить обработку исключений конвертирования типов
                Time = worksheet.Cell(row, 1).GetDouble(),
                LeftEye = worksheet.Cell(row, LeftEyeColumnNum).GetDouble(),
                RigthEye = worksheet.Cell(row, RightEyeColumnNum).GetDouble(),
            };
            // И возвращаем его
            yield return metric;
        }
        yield break;
    }
    */

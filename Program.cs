using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;



namespace ConsoleApplication2
{
    class Program
    {

  // Забираем даты 1-ого столбца исх.файлы, где время 23-30 - получаем массив Date!
        public static List <int> GetDateFromFile(string NameFileIn)
        {
            StreamReader fileIn = new StreamReader(NameFileIn);
            List<int> Date = new List<int>();
            int dat = 0;
            int tim = 0;

            while (!fileIn.EndOfStream)
            {
                string[] line = fileIn.ReadLine().Split(';');
                int.TryParse(line[2], out dat);
                int.TryParse(line[3], out tim);
                if (tim == 233000) Date.Add(dat);
            }
            fileIn.Close();
            return Date;
        }


 // Забираем эквити 9-ого столбца исх.файла,где время 23-30,делаем из него Returns - записываем в массив!
        public static  List <int> GetReturnsFromFile(string NameFileIn, int weightKoef)
        {
            StreamReader fileIn = new StreamReader(NameFileIn);
            List<int> Equity = new List<int>();
            List<int> Returns = new List<int>();
            int eq = 0;
            int tim = 0;
            while (!fileIn.EndOfStream)
            {
                string[] line = fileIn.ReadLine().Split(';');
                int.TryParse(line[3], out tim);
                int.TryParse(line[9], out eq);
                if (tim == 233000) Equity.Add(eq);
            }
            Returns.Add(0);
            for (int i = 1; i < Equity.Count; i++)
            {
                Returns.Add((Equity[i] - Equity[i - 1])*weightKoef);
            }
            fileIn.Close();
            return Returns;
        }

// Вывод всех показателей из общего ретерна в файл
        public static void WriteFileFromReturns(List<int>Date, List<int> Returns, int minKapital, int weightKoef, string NameFileOut) 
            {
           // Расчет нового DayEquity на основе Return*вес стратегии...
            List<int> DayEquity = new List<int>();
            int sum = 0;
            for (int i = 0; i < Returns.Count; i++)
            {
                sum = sum + Returns[i];
                DayEquity.Add(sum);
            }
           // Расчет подневного RETURNS в процентах 
            List<double> ReturnsInt = new List<double>();
            for (int i = 0; i < Returns.Count; i++)
            {
                ReturnsInt.Add(100 * Convert.ToDouble(Returns[i]) / (DayEquity[i] + (minKapital*weightKoef)));
            }
            // Расчет maxEquity максимальных значений эквити в массив 
            List<int> maxEquity = new List<int>();
            int max = 0;
            for (int j = 0; j < DayEquity.Count; j++)
            {
                if (DayEquity[j] > max) { max = DayEquity[j]; }
                maxEquity.Add(max);
            }
            // Расчет текущей просадки (посл.макс.значение эквити минус текущее эквити)
            List<int> DrawDn = new List<int>();
            for (int j = 0; j < DayEquity.Count; j++) { DrawDn.Add(maxEquity[j] - DayEquity[j]); }
            int maxDrawDn = DrawDn.Max();
            // Расчет текущей просадки в %...
            List<double> DrawDnInt = new List<double>();
            for (int j = 0; j < DrawDn.Count; j++) { DrawDnInt.Add(100 * Convert.ToDouble(DrawDn[j]) / (maxEquity[j] + (minKapital*weightKoef))); }
            double maxDrawDnInt = DrawDnInt.Max();
           // Расчет длительности всех просадoк (дни)....
            List<int> DurationDn = new List<int>();
            int n = 0;
            for (int i = 0; i < DrawDn.Count; i++)
            {
                if (DrawDn[i] > 0) { n = n + 1; DurationDn.Add(n); }
                if (DrawDn[i] == 0) { n = 0; DurationDn.Add(n); }
            }
            int maxDuration = DurationDn.Max();
            // Расчет среднегодовой доходности, % ... (весь доход делим на количество лет....
            // и делим на цену первой свечи и умнож. на сто))
            double AvAnnIncome = 100.0 * (DayEquity[DayEquity.Count - 1] / (Convert.ToDouble(DayEquity.Count) / 252)) / (minKapital*weightKoef);
            // СОРТИНО: Расчет среднеквадр.отклонения ТОЛЬКО просадок, только минусов..
            // считается в процентах: среднегод.доход делим на ст.отклонение(в знамен: n-1)!, которое:
            // сумма всех квадратов разностей тек.значений и среднего арифм., деленной на n значений - и все под корнем.
            double sortino = 0, sum1 = 0, sum2 = 0;
            int RiskFreeRate = 10; // Безрисковая ставка, %           
            for (int i = 0; i < ReturnsInt.Count; i++)
            {
                if (ReturnsInt[i] < 0) { sum1 = sum1 + ReturnsInt[i]; }
            }
            double avReturnInt = sum1 / ReturnsInt.Count; // среднее значение просадок в %
            for (int i = 0; i < ReturnsInt.Count; i++)
            {
                if (ReturnsInt[i] < 0) { sum2 = sum2 + Math.Pow((ReturnsInt[i] - avReturnInt), 2); }
            }
            double StOtklon = Math.Sqrt(252 * sum2 / (ReturnsInt.Count));  // Стандартное отклонение посчитано
            sortino = (AvAnnIncome - RiskFreeRate) / StOtklon;
           // ЗАПИСЬ В файл ежедн-ых даты, еquity, maxEquity, Drawdown, DurationDn...
            StreamWriter fileOut = new StreamWriter(NameFileOut, false);
            fileOut.WriteLine("Date; DayReturns* " + weightKoef + "; ReturnsInt%" + "; DayEquity; maxEquity; DrawDown(rub):" + maxDrawDn
                + "; DrawDown%:" + Math.Round(maxDrawDnInt, 2) + "; DurationDn(days):" + maxDuration
                + "; AvAnnIncome%:" + Math.Round(AvAnnIncome, 2) + "; " + "Sortino:" + Math.Round(sortino, 2)
                + "; MinKapital(rub):" + minKapital*weightKoef);
            for (int j = 0; j < Date.Count; j++)
            {
                fileOut.WriteLine(Date[j] + "; " + Returns[j] + ";" + Math.Round(ReturnsInt[j], 2) + "; " + DayEquity[j] + "; " + maxEquity[j]
                    + "; " + DrawDn[j] + "; " + Math.Round(DrawDnInt[j], 2) + "; " + DurationDn[j]);
            }
            fileOut.Close();
        }


 // Удаление дубликатов и ненужных необщих дат за пределами выбранн.диапазона и сортировка 
        public static List<int> GetCommonDates2(List<int> CommonDates, List<int> FirstDates, List<int> LastDates)
        {
            for (int i = 0; i < CommonDates.Count; i++)    // Удаление дубликатов
            {
                for (int j = 0; j < CommonDates.Count; j++)
                { if (i != j && CommonDates[i] == CommonDates[j]) { CommonDates.RemoveAt(j); } }
            }
            for (int i = 0; i < CommonDates.Count; i++)    // Удаление дубликатов еще раз))
            {
                for (int j = 0; j < CommonDates.Count; j++)
                { if (i != j && CommonDates[i] == CommonDates[j]) { CommonDates.RemoveAt(j); } }
            }
            CommonDates.Sort(); // Сортировка всех дат
            List<int> CommonDates2 = new List<int>();
            for (int i = 0; i < CommonDates.Count; i++)  // Удаление дат не общих для всех страт
            {
                if (CommonDates[i] >= FirstDates.Max() && CommonDates[i] <= LastDates.Min()) { CommonDates2.Add(CommonDates[i]); }
            }

            return CommonDates2;
        }


 // Исправление в датах страты периода общих дат в соответствии с датами CommonDates: добавка дат, еси пропущено...   
        public static List<int> GetRightDate(List <int> Date, List<int> Returns, List <int> FirstDates, List <int> CommonDates)
        {
            int a = 0;
            int b = Date.IndexOf(FirstDates.Max());
            while (a != CommonDates.Count - 1)
            {
                if (CommonDates[a] == Date[b]) { a++; b++; }
                if (CommonDates[a] != Date[b])
                {
                    Date.Insert(b, CommonDates[a]);
                    Returns.Insert(b, 0);
                    a++; b++;
                }
            }
            return Date;
        }


// Исправление в Ретернах страты периода ретернов в соответствии с датами CommonDates: добавка нулей...   
        public static List<int> GetRightReturns(List<int> Date, List<int> Returns, List<int> FirstDates, List<int> CommonDates)
        {
            int a = 0;
            int b = Date.IndexOf(FirstDates.Max());
            while (a != CommonDates.Count - 1)
            {
                if (CommonDates[a] == Date[b]) { a++; b++; }
                if (CommonDates[a] != Date[b])
                {
                    Date.Insert(b, CommonDates[a]);
                    Returns.Insert(b, 0);
                    a++; b++;
                }
            }
            return Returns;
        }

// Складываем ретерны 2 страт и получаем Returns общие...
     public static List<int> GetCommonReturns2(List<int> Date1, List<int> Date2, List<int> Returns1, List<int> Returns2, List<int> FirstDates, List<int> CommonDates)
        {
            List<int> Returns = new List <int>();
            int c = 0;
            int d = Date1.IndexOf(FirstDates.Max());
            int e = Date2.IndexOf(FirstDates.Max());
            while (c != CommonDates.Count)
            {
                if (CommonDates[c] == Date1[d] && CommonDates[c] == Date2[e])
                {
                    Returns.Add(Returns1[d] + Returns2[e]);
                }
                c++; d++; e++;
            }
            return Returns;
        }

// Складываем ретерны 3 страт и получаем Returns общие...
        public static List<int> GetCommonReturns3(List<int> Date1, List<int> Date2, List<int> Date3, List<int> Returns1, List<int> Returns2, List<int> Returns3, List<int> FirstDates, List<int> CommonDates)
        {
            List<int> Returns = new List<int>();
            int c = 0;
            int d = Date1.IndexOf(FirstDates.Max());
            int e = Date2.IndexOf(FirstDates.Max());
            int f = Date3.IndexOf(FirstDates.Max());
            while (c != CommonDates.Count)
            {
                if (CommonDates[c] == Date1[d] && CommonDates[c] == Date2[e] && CommonDates[c] == Date3[f]) 
                {
                    Returns.Add(Returns1[d] + Returns2[e] + Returns3[f]);
                }
                c++; d++; e++; f++;
            }
            return Returns;
        }


// Складываем ретерны 7 страт и получаем Returns общие...
        public static List<int> GetCommonReturns7(List<int> Date1, List<int> Date2, List<int> Date3, List<int> Date4, 
            List<int> Date5, List<int> Date6, List<int> Date7,List<int> Returns1, List<int> Returns2, List<int> Returns3, 
            List<int> Returns4, List<int> Returns5, List<int> Returns6, List<int> Returns7, List<int> FirstDates, List<int> CommonDates)
        {
            List<int> Returns = new List<int>();
            int c = 0;
            int d = Date1.IndexOf(FirstDates.Max());
            int e = Date2.IndexOf(FirstDates.Max());
            int f = Date3.IndexOf(FirstDates.Max());
            int g = Date4.IndexOf(FirstDates.Max());
            int h = Date5.IndexOf(FirstDates.Max());
            int i = Date6.IndexOf(FirstDates.Max());
            int j = Date7.IndexOf(FirstDates.Max());
            while (c != CommonDates.Count)
            {
                if (CommonDates[c] == Date1[d] && CommonDates[c] == Date2[e] && CommonDates[c] == Date3[f] 
                    && CommonDates[c] == Date4[g] && CommonDates[c] == Date5[h] && CommonDates[c] == Date6[i] && CommonDates[c] == Date7[j])
                {
                    Returns.Add(Returns1[d] + Returns2[e] + Returns3[f] + Returns4[g] + Returns5[h] + Returns6[i] + Returns7[j]);
                }
                c++; d++; e++; f++; g++; h++; i++; j++;
            }
            return Returns;
        }


// Складываем ретерны 10 страт и получаем Returns общие...
        public static List<int> GetCommonReturns10(List<int> Date1, List<int> Date2, List<int> Date3, List<int> Date4,
            List<int> Date5, List<int> Date6, List<int> Date7, List<int> Date8, List<int> Date9, List<int> Date10,
            List<int> Returns1, List<int> Returns2, List<int> Returns3, List<int> Returns4, List<int> Returns5, 
            List<int> Returns6, List<int> Returns7, List<int> Returns8, List<int> Returns9, List<int> Returns10, 
            List<int> FirstDates, List<int> CommonDates)
        {
            List<int> Returns = new List<int>();
            int c = 0;
            int d = Date1.IndexOf(FirstDates.Max());
            int e = Date2.IndexOf(FirstDates.Max());
            int f = Date3.IndexOf(FirstDates.Max());
            int g = Date4.IndexOf(FirstDates.Max());
            int h = Date5.IndexOf(FirstDates.Max());
            int i = Date6.IndexOf(FirstDates.Max());
            int j = Date7.IndexOf(FirstDates.Max());
            int k = Date8.IndexOf(FirstDates.Max());
            int l = Date9.IndexOf(FirstDates.Max());
            int m = Date10.IndexOf(FirstDates.Max());
            while (c != CommonDates.Count)
            {
                if (CommonDates[c] == Date1[d] && CommonDates[c] == Date2[e] && CommonDates[c] == Date3[f]
                    && CommonDates[c] == Date4[g] && CommonDates[c] == Date5[h] && CommonDates[c] == Date6[i] && CommonDates[c] == Date7[j] 
                    && CommonDates[c] == Date8[k] && CommonDates[c] == Date9[l] && CommonDates[c] == Date10[m])
                {
                    Returns.Add(Returns1[d] + Returns2[e] + Returns3[f] + Returns4[g] + Returns5[h] + Returns6[i] 
                        + Returns7[j] + Returns8[k] + Returns9[l] + Returns10[m]);
                }
                c++; d++; e++; f++; g++; h++; i++; j++; k++; l++; m++;
            }
            return Returns;
        }


 // Вывод всех показателей из общего ретерна в один файл построчно...
        public static void WriteFileAnaliz7(List<int> Date, List<int> Returns, int minKapital, string NameFileOut, 
            int weight1, int weight2, int weight3, int weight4, int weight5, int weight6, int weight7)
        {
            // Расчет нового DayEquity на основе Return*вес стратегии...
            List<int> DayEquity = new List<int>();
            int sum = 0;
            for (int i = 0; i < Returns.Count; i++)
            {
                sum = sum + Returns[i];
                DayEquity.Add(sum);
            }
            // Расчет подневного RETURNS в процентах 
            List<double> ReturnsInt = new List<double>();
            for (int i = 0; i < Returns.Count; i++)
            {
                ReturnsInt.Add(100 * Convert.ToDouble(Returns[i]) / (DayEquity[i] + (minKapital)));
            }
            // Расчет maxEquity максимальных значений эквити в массив 
            List<int> maxEquity = new List<int>();
            int max = 0;
            for (int j = 0; j < DayEquity.Count; j++)
            {
                if (DayEquity[j] > max) { max = DayEquity[j]; }
                maxEquity.Add(max);
            }
            // Расчет текущей просадки (посл.макс.значение эквити минус текущее эквити)
            List<int> DrawDn = new List<int>();
            for (int j = 0; j < DayEquity.Count; j++) { DrawDn.Add(maxEquity[j] - DayEquity[j]); }
            int maxDrawDn = DrawDn.Max();
            // Расчет текущей просадки в %...
            List<double> DrawDnInt = new List<double>();
            for (int j = 0; j < DrawDn.Count; j++) { DrawDnInt.Add(100 * Convert.ToDouble(DrawDn[j]) / (maxEquity[j] + minKapital)); }
            double maxDrawDnInt = DrawDnInt.Max();
            // Расчет длительности всех просадoк (дни)....
            List<int> DurationDn = new List<int>();
            int n = 0;
            for (int i = 0; i < DrawDn.Count; i++)
            {
                if (DrawDn[i] > 0) { n = n + 1; DurationDn.Add(n); }
                if (DrawDn[i] == 0) { n = 0; DurationDn.Add(n); }
            }
            int maxDuration = DurationDn.Max();
            // Расчет среднегодовой доходности, % ... (весь доход делим на количество лет....
            // и делим на цену первой свечи и умнож. на сто))
            double AvAnnIncome = 100.0 * (DayEquity[DayEquity.Count - 1] / (Convert.ToDouble(DayEquity.Count) / 252)) / minKapital;
            // СОРТИНО: Расчет среднеквадр.отклонения ТОЛЬКО просадок, только минусов..
            // считается в процентах: среднегод.доход делим на ст.отклонение(в знамен: n-1)!, которое:
            // сумма всех квадратов разностей тек.значений и среднего арифм., деленной на n значений - и все под корнем.
            double sortino = 0, sum1 = 0, sum2 = 0;
            int RiskFreeRate = 7; // Безрисковая ставка, %           
            for (int i = 0; i < ReturnsInt.Count; i++)
            {
                if (ReturnsInt[i] < 0) { sum1 = sum1 + ReturnsInt[i]; }
            }
            double avReturnInt = sum1 / ReturnsInt.Count; // среднее значение просадок в %
            for (int i = 0; i < ReturnsInt.Count; i++)
            {
                if (ReturnsInt[i] < 0) { sum2 = sum2 + Math.Pow((ReturnsInt[i] - avReturnInt), 2); }
            }
            double StOtklon = Math.Sqrt(252 * sum2 / (ReturnsInt.Count));  // Стандартное отклонение посчитано
            sortino = (AvAnnIncome - RiskFreeRate) / StOtklon;
            // ЗАПИСЬ В файл ежедн-ых даты, еquity, maxEquity, Drawdown, DurationDn...
            StreamWriter fileOut = new StreamWriter(NameFileOut, true);
            fileOut.WriteLine(Date[0] + "; " + Date[Date.Count - 1] + ";" + maxDrawDn + "; "
                + Math.Round(DrawDnInt.Max(), 2) + "; " + maxDuration + "; " + Math.Round(AvAnnIncome, 2) 
                + "; " + Math.Round(sortino, 2) + "; " + minKapital + "; " + weight1 + "; " + weight2 + "; " + weight3 
                + "; " + weight4 + "; " + weight5 + "; " + weight6 + "; " + weight7);
            
            fileOut.Close();
        }

        // Вывод всех показателей из общего ретерна в один файл построчно...
        public static void WriteFileAnaliz10(List<int> Date, List<int> Returns, int minKapital, string NameFileOut,
            int weight1, int weight2, int weight3, int weight4, int weight5, int weight6, int weight7, int weight8, 
            int weight9, int weight10)
        {
            // Расчет нового DayEquity на основе Return*вес стратегии...
            List<int> DayEquity = new List<int>();
            int sum = 0;
            for (int i = 0; i < Returns.Count; i++)
            {
                sum = sum + Returns[i];
                DayEquity.Add(sum);
            }
            // Расчет подневного RETURNS в процентах 
            List<double> ReturnsInt = new List<double>();
            for (int i = 0; i < Returns.Count; i++)
            {
                ReturnsInt.Add(100 * Convert.ToDouble(Returns[i]) / (DayEquity[i] + (minKapital)));
            }
            // Расчет maxEquity максимальных значений эквити в массив 
            List<int> maxEquity = new List<int>();
            int max = 0;
            for (int j = 0; j < DayEquity.Count; j++)
            {
                if (DayEquity[j] > max) { max = DayEquity[j]; }
                maxEquity.Add(max);
            }
            // Расчет текущей просадки (посл.макс.значение эквити минус текущее эквити)
            List<int> DrawDn = new List<int>();
            for (int j = 0; j < DayEquity.Count; j++) { DrawDn.Add(maxEquity[j] - DayEquity[j]); }
            int maxDrawDn = DrawDn.Max();
            // Расчет текущей просадки в %...
            List<double> DrawDnInt = new List<double>();
            for (int j = 0; j < DrawDn.Count; j++) { DrawDnInt.Add(100 * Convert.ToDouble(DrawDn[j]) / (maxEquity[j] + minKapital)); }
            double maxDrawDnInt = DrawDnInt.Max();
            // Расчет длительности всех просадoк (дни)....
            List<int> DurationDn = new List<int>();
            int n = 0;
            for (int i = 0; i < DrawDn.Count; i++)
            {
                if (DrawDn[i] > 0) { n = n + 1; DurationDn.Add(n); }
                if (DrawDn[i] == 0) { n = 0; DurationDn.Add(n); }
            }
            int maxDuration = DurationDn.Max();
            // Расчет среднегодовой доходности, % ... (весь доход делим на количество лет....
            // и делим на цену первой свечи и умнож. на сто))
            double AvAnnIncome = 100.0 * (DayEquity[DayEquity.Count - 1] / (Convert.ToDouble(DayEquity.Count) / 252)) / minKapital;
            // СОРТИНО: Расчет среднеквадр.отклонения ТОЛЬКО просадок, только минусов..
            // считается в процентах: среднегод.доход делим на ст.отклонение(в знамен: n-1)!, которое:
            // сумма всех квадратов разностей тек.значений и среднего арифм., деленной на n значений - и все под корнем.
            double sortino = 0, sum1 = 0, sum2 = 0;
            int RiskFreeRate = 7; // Безрисковая ставка, %           
            for (int i = 0; i < ReturnsInt.Count; i++)
            {
                if (ReturnsInt[i] < 0) { sum1 = sum1 + ReturnsInt[i]; }
            }
            double avReturnInt = sum1 / ReturnsInt.Count; // среднее значение просадок в %
            for (int i = 0; i < ReturnsInt.Count; i++)
            {
                if (ReturnsInt[i] < 0) { sum2 = sum2 + Math.Pow((ReturnsInt[i] - avReturnInt), 2); }
            }
            double StOtklon = Math.Sqrt(252 * sum2 / (ReturnsInt.Count));  // Стандартное отклонение посчитано
            sortino = (AvAnnIncome - RiskFreeRate) / StOtklon;
            // ЗАПИСЬ В файл ежедн-ых даты, еquity, maxEquity, Drawdown, DurationDn...
            StreamWriter fileOut = new StreamWriter(NameFileOut, true);
            fileOut.WriteLine(Date[0] + "; " + Date[Date.Count - 1] + ";" + maxDrawDn + "; "
                + Math.Round(DrawDnInt.Max(), 2) + "; " + maxDuration + "; " + Math.Round(AvAnnIncome, 2)
                + "; " + Math.Round(sortino, 2) + "; " + minKapital + "; " + weight1 + "; " + weight2 + "; " + weight3
                + "; " + weight4 + "; " + weight5 + "; " + weight6 + "; " + weight7 + "; " + weight8 + "; " + weight9 + "; " + weight10);

            fileOut.Close();
        }




        //-----------------------------------------------------------------------------------------------------------------------------------

        static void Main(string[] args)
        {

            StreamWriter fileOut = new StreamWriter("List_Portfolio.txt", true);
            fileOut.WriteLine("Начало; Конец; Макс.просадка,р); Макс.просадка%; Макс.Длит.просады; Ср.год.доход; Сортино; Капитал,р; StockEmaRi; ProboySerDayRi; 3SMAfractalRi; ARB-RiSi; MaxMiDaySb; VynosySb; ProboyBokSb; ProboySi; StrokozaSi; MacdSi");
            fileOut.Close();

            for (int r = 1; r < 2; r++)  //220 000
            {
                for (int g = 1; g < 4; g++)   //110 000
                {
                    for (int h = 1; h < 4; h++)   //  110 000
                    {
                        for (int l = 1; l < 2; l++)   // 350 000
                        {
                            for (int p = 1; p < 4; p++)  //  116 000
                            {
                                for (int q = 1; q < 3; q++) // 220 000
                                {
                                    for (int s = 1; s < 2; s++)   // 280 000
                                    {
                                        for (int t = 1; t < 2; t++) // 300 000
                                        {
                                            for (int u = 1; u < 3; u++) // 120 000
                                            { 
                                                for (int w = 1; w < 2; w++) // 300 000
                                                {
                                                    List<int> FirstDates = new List<int>(); // для поиска общей минимальной даты
                                                    List<int> LastDates = new List<int>(); // для поиска общей максимальной даты
                                                    List<int> CommonDates = new List<int>(); // Для создания общих дат
                                                    List<int> Returns = new List<int>();  // Для создания общих ретернов
                                                    int SumKapital = 0; // Суммарный капитал всех страт в портфеле с учетом весов каждой страты))

                                                    //    Страта 1 StockEmaRi:
                                                    int minKapital1 = 220000;
                                                    int weightKoef1 = r;
                                                    SumKapital = SumKapital + minKapital1 * weightKoef1;
                                                    List<int> Date1 = GetDateFromFile("StockEmaRi.txt");
                                                    List<int> Returns1 = GetReturnsFromFile("StockEmaRi.txt", weightKoef1);
                                                    //  WriteFileFromReturns(Date1, Returns1, minKapital1, weightKoef1, "StockEmaRi_Rez.txt");
                                                    FirstDates.Add(Date1[0]);
                                                    LastDates.Add(Date1[Date1.Count - 1]);
                                                    for (int i = 0; i < Date1.Count; i++) { CommonDates.Add(Date1[i]); }

                                                    //    Стратегия 2 ProboySerDayRi:
                                                    int minKapital2 = 110000;
                                                    int weightKoef2 = g;
                                                    SumKapital = SumKapital + minKapital2 * weightKoef2;
                                                    List<int> Date2 = GetDateFromFile("ProboySerDayRi.txt");
                                                    List<int> Returns2 = GetReturnsFromFile("ProboySerDayRi.txt", weightKoef2);
                                                    //   WriteFileFromReturns(Date2, Returns2, minKapital2, weightKoef2, "ProboySerDayRi_Rez.txt");
                                                    FirstDates.Add(Date2[0]);
                                                    LastDates.Add(Date2[Date2.Count - 1]);
                                                    for (int i = 0; i < Date2.Count; i++) { CommonDates.Add(Date2[i]); }

                                                    //   Стратегия 3 3SMAfractalRi:
                                                    int minKapital3 = 110000;
                                                    int weightKoef3 = h;
                                                    SumKapital = SumKapital + minKapital3 * weightKoef3;
                                                    List<int> Date3 = GetDateFromFile("3SMAfractalRi.txt");
                                                    List<int> Returns3 = GetReturnsFromFile("3SMAfractalRi.txt", weightKoef3);
                                                    //   WriteFileFromReturns(Date3, Returns3, minKapital3, weightKoef3, "3SMAfractalRi_Rez.txt");
                                                    FirstDates.Add(Date3[0]);
                                                    LastDates.Add(Date3[Date3.Count - 1]);
                                                    for (int i = 0; i < Date3.Count; i++) { CommonDates.Add(Date3[i]); }

                                                    // Стратегия 4 ARB-RiSi
                                                    int minKapital4 = 350000;
                                                    int weightKoef4 = l;
                                                    SumKapital = SumKapital + minKapital4 * weightKoef4;
                                                    List<int> Date4 = GetDateFromFile("ARB-RiSi.txt");
                                                    List<int> Returns4 = GetReturnsFromFile("ARB-RiSi.txt", weightKoef4);
                                                    //    WriteFileFromReturns(Date4, Returns4, minKapital4, weightKoef4, "ARB-RiSi_Rez.txt");
                                                    FirstDates.Add(Date4[0]);
                                                    LastDates.Add(Date4[Date4.Count - 1]);
                                                    for (int i = 0; i < Date4.Count; i++) { CommonDates.Add(Date4[i]); }

                                                    // Стратегия 5   MaxMiDaySb
                                                    int minKapital5 = 116000;
                                                    int weightKoef5 = p;
                                                    SumKapital = SumKapital + minKapital5 * weightKoef5;
                                                    List<int> Date5 = GetDateFromFile("MaxMiDaySb.txt");
                                                    List<int> Returns5 = GetReturnsFromFile("MaxMiDaySb.txt", weightKoef5);
                                                    //    WriteFileFromReturns(Date5, Returns5, minKapital5, weightKoef5, "MaxMiDaySb_Rez.txt");
                                                    FirstDates.Add(Date5[0]);
                                                    LastDates.Add(Date5[Date5.Count - 1]);
                                                    for (int i = 0; i < Date5.Count; i++) { CommonDates.Add(Date5[i]); }

                                                    // Стратегия 6   VynosySb
                                                    int minKapital6 = 220000;
                                                    int weightKoef6 = q;
                                                    SumKapital = SumKapital + minKapital6 * weightKoef6;
                                                    List<int> Date6 = GetDateFromFile("VynosySb.txt");
                                                    List<int> Returns6 = GetReturnsFromFile("VynosySb.txt", weightKoef6);
                                                    //     WriteFileFromReturns(Date6, Returns6, minKapital6, weightKoef6, "VynosySb_Rez.txt");
                                                    FirstDates.Add(Date6[0]);
                                                    LastDates.Add(Date6[Date6.Count - 1]);
                                                    for (int i = 0; i < Date6.Count; i++) { CommonDates.Add(Date6[i]); }

                                                    // Стратегия 7   ProboyBokSb
                                                    int minKapital7 = 280000;
                                                    int weightKoef7 = s;
                                                    SumKapital = SumKapital + minKapital7 * weightKoef7;
                                                    List<int> Date7 = GetDateFromFile("ProboyBokSb.txt");
                                                    List<int> Returns7 = GetReturnsFromFile("ProboyBokSb.txt", weightKoef7);
                                                    //    WriteFileFromReturns(Date7, Returns7, minKapital7, weightKoef7, "ProboyBokSb_Rez.txt");
                                                    FirstDates.Add(Date7[0]);
                                                    LastDates.Add(Date7[Date7.Count - 1]);
                                                    for (int i = 0; i < Date7.Count; i++) { CommonDates.Add(Date7[i]); }

                                                    // Стратегия 8    ProboySi
                                                    int minKapital8 = 300000;
                                                    int weightKoef8 = t;
                                                    SumKapital = SumKapital + minKapital8 * weightKoef8;
                                                    List<int> Date8 = GetDateFromFile("ProboySi.txt");
                                                    List<int> Returns8 = GetReturnsFromFile("ProboySi.txt", weightKoef8);
                                                    //    WriteFileFromReturns(Date8, Returns8, minKapital8, weightKoef8, "ProboySi_Rez.txt");
                                                    FirstDates.Add(Date8[0]);
                                                    LastDates.Add(Date8[Date8.Count - 1]);
                                                    for (int i = 0; i < Date8.Count; i++) { CommonDates.Add(Date8[i]); }


                                                    // Стратегия 9    StrekozaSi
                                                    int minKapital9 = 120000;
                                                    int weightKoef9 = u;
                                                    SumKapital = SumKapital + minKapital9 * weightKoef9;
                                                    List<int> Date9 = GetDateFromFile("StrekozaSi.txt");
                                                    List<int> Returns9 = GetReturnsFromFile("StrekozaSi.txt", weightKoef9);
                                                    //    WriteFileFromReturns(Date9, Returns9, minKapital9, weightKoef9, "StrekozaSi_Rez.txt");
                                                    FirstDates.Add(Date9[0]);
                                                    LastDates.Add(Date9[Date9.Count - 1]);
                                                    for (int i = 0; i < Date9.Count; i++) { CommonDates.Add(Date9[i]); }


                                                    // Стратегия 10    MacdSi
                                                    int minKapital10 = 300000;
                                                    int weightKoef10 = w;
                                                    SumKapital = SumKapital + minKapital10 * weightKoef10;
                                                    List<int> Date10 = GetDateFromFile("MacdSi.txt");
                                                    List<int> Returns10 = GetReturnsFromFile("MacdSi.txt", weightKoef10);
                                                    //    WriteFileFromReturns(Date10, Returns10, minKapital10, weightKoef10, "MacdSi_Rez.txt");
                                                    FirstDates.Add(Date10[0]);
                                                    LastDates.Add(Date10[Date10.Count - 1]);
                                                    for (int i = 0; i < Date10.Count; i++) { CommonDates.Add(Date10[i]); }



                                                    if (SumKapital > 2000000 && SumKapital < 3500000)  // от 500 000 до 1 000 000 
                                                    {
                                                        // Установка любой даты начала и конца теста портфеля!
                                                        //FirstDates.Clear();
                                                        //FirstDates.Add(20150105);
                                                        //LastDates.Clear();
                                                        //LastDates.Add(20161230);

                                                        // Создание единого массива ДАТ, взятого от всех страт с удалением дубликатов и сортировкой по возрастанию
                                                        CommonDates = GetCommonDates2(CommonDates, FirstDates, LastDates);

                                                        // Создание отредактированных ретернов и дат в страте 2  внутри промежутка общих дат
                                                        Date1 = GetRightDate(Date1, Returns1, FirstDates, CommonDates);
                                                        Returns1 = GetRightReturns(Date1, Returns1, FirstDates, CommonDates);

                                                        // Создание отредактированных ретернов и дат в страте 1 внутри промежутка общих дат
                                                        Date2 = GetRightDate(Date2, Returns2, FirstDates, CommonDates);
                                                        Returns2 = GetRightReturns(Date2, Returns2, FirstDates, CommonDates);

                                                        // Создание отредактированных ретернов и дат в страте 3  внутри промежутка общих дат
                                                        Date3 = GetRightDate(Date3, Returns3, FirstDates, CommonDates);
                                                        Returns3 = GetRightReturns(Date3, Returns3, FirstDates, CommonDates);

                                                        // Создание отредактированных ретернов и дат в страте 4  внутри промежутка общих дат
                                                        Date4 = GetRightDate(Date4, Returns4, FirstDates, CommonDates);
                                                        Returns4 = GetRightReturns(Date4, Returns4, FirstDates, CommonDates);

                                                        //  Создание отредактированных ретернов и дат в страте 5  внутри промежутка общих дат
                                                        Date5 = GetRightDate(Date5, Returns5, FirstDates, CommonDates);
                                                        Returns5 = GetRightReturns(Date5, Returns5, FirstDates, CommonDates);

                                                        // Создание отредактированных ретернов и дат в страте 6  внутри промежутка общих дат
                                                        Date6 = GetRightDate(Date6, Returns6, FirstDates, CommonDates);
                                                        Returns6 = GetRightReturns(Date6, Returns6, FirstDates, CommonDates);

                                                        // Создание отредактированных ретернов и дат в страте 7  внутри промежутка общих дат
                                                        Date7 = GetRightDate(Date7, Returns7, FirstDates, CommonDates);
                                                        Returns7 = GetRightReturns(Date7, Returns7, FirstDates, CommonDates);

                                                        // Создание отредактированных ретернов и дат в страте 8  внутри промежутка общих дат
                                                        Date8 = GetRightDate(Date8, Returns8, FirstDates, CommonDates);
                                                        Returns8 = GetRightReturns(Date8, Returns8, FirstDates, CommonDates);

                                                        // Создание отредактированных ретернов и дат в страте 9  внутри промежутка общих дат
                                                        Date9 = GetRightDate(Date9, Returns9, FirstDates, CommonDates);
                                                        Returns9 = GetRightReturns(Date9, Returns9, FirstDates, CommonDates);

                                                        // Создание отредактированных ретернов и дат в страте 10  внутри промежутка общих дат
                                                        Date10 = GetRightDate(Date10, Returns10, FirstDates, CommonDates);
                                                        Returns10 = GetRightReturns(Date10, Returns10, FirstDates, CommonDates);

                                                        // Складываем ретерны и получаем Returns общие... кол-во элементов как в CommonDates
                                                        Returns = GetCommonReturns10(Date1, Date2, Date3, Date4, Date5, Date6, Date7, Date8, Date9, Date10,
                                                        Returns1, Returns2, Returns3, Returns4, Returns5, Returns6, Returns7, Returns8, Returns9, Returns10,
                                                        FirstDates, CommonDates);

                                                        //// Запись в файл каждую строку основных параметров
                                                        WriteFileAnaliz10(CommonDates, Returns, SumKapital, "List_Portfolio.txt", weightKoef1, weightKoef2,
                                                            weightKoef3, weightKoef4, weightKoef5, weightKoef6, weightKoef7, weightKoef8,weightKoef9,weightKoef10);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            //StreamWriter fileSumReturns = new StreamWriter("SumRetuns.txt", false);
            //for (int i = 0; i < CommonDates.Count; i++)
            //{
            //    fileSumReturns.WriteLine(CommonDates[i]);
            //    fileSumReturns.WriteLine(Date5[i]);
            //}
            //fileSumReturns.Close();


            // Получаем из суммарного Returns -Эквити и прочие показатели, записываем в файл Portfolio 
            // weightkoef всегда = 1, т.к. это портфель с уже развесов.ретернами внутри, и его вес еще раз не умножаем
            // SumKapital есть суммарный капитал для портфеля, по сумме суммарного капитала на каждую страту))
            //     WriteFileFromReturns(CommonDates, Returns, SumKapital, 1, "Portfolio_Rez.txt");

            // РИСУЕМ ГРАФИКи
            //form1 frm = new form1();
            //Application.Run(frm);


            //НАСТРОЙКИ 7-МИ ПАРНЫХ СТРАТ

            ////    Страта 1 Si+SBer:
            //int minKapital1 = 130000;
            //int weightKoef1 = r;
            //SumKapital = SumKapital + minKapital1 * weightKoef1;
            //List<int> Date1 = GetDateFromFile("Si+SBer.txt");
            //List<int> Returns1 = GetReturnsFromFile("Si+SBer.txt", weightKoef1);
            ////  WriteFileFromReturns(Date1, Returns1, minKapital1, weightKoef1, "Si+SBer_Rez.txt");
            //FirstDates.Add(Date1[0]);
            //LastDates.Add(Date1[Date1.Count - 1]);
            //for (int i = 0; i < Date1.Count; i++) { CommonDates.Add(Date1[i]); }

            ////    Стратегия 2 VTB+SBpr:
            //int minKapital2 = 20000;
            //int weightKoef2 = g;
            //SumKapital = SumKapital + minKapital2 * weightKoef2;
            //List<int> Date2 = GetDateFromFile("VTB+SBpr.txt");
            //List<int> Returns2 = GetReturnsFromFile("VTB+SBpr.txt", weightKoef2);
            ////   WriteFileFromReturns(Date2, Returns2, minKapital2, weightKoef2, "VTB+SBpr_Rez.txt");
            //FirstDates.Add(Date2[0]);
            //LastDates.Add(Date2[Date2.Count - 1]);
            //for (int i = 0; i < Date2.Count; i++) { CommonDates.Add(Date2[i]); }

            ////   Стратегия 3 SBPR+GPPU:
            //int minKapital3 = 23000;
            //int weightKoef3 = h;
            //SumKapital = SumKapital + minKapital3 * weightKoef3;
            //List<int> Date3 = GetDateFromFile("SBPR+GBPU.txt");
            //List<int> Returns3 = GetReturnsFromFile("SBPR+GBPU.txt", weightKoef3);
            ////   WriteFileFromReturns(Date3, Returns3, minKapital3, weightKoef3, "SBPR+GBPU_Rez.txt");
            //FirstDates.Add(Date3[0]);
            //LastDates.Add(Date3[Date3.Count - 1]);
            //for (int i = 0; i < Date3.Count; i++) { CommonDates.Add(Date3[i]); }

            //// Стратегия 4 LKOH+MIX
            //int minKapital4 = 60000;
            //int weightKoef4 = l;
            //SumKapital = SumKapital + minKapital4 * weightKoef4;
            //List<int> Date4 = GetDateFromFile("LKOH+MIX.txt");
            //List<int> Returns4 = GetReturnsFromFile("SBPR+GBPU.txt", weightKoef4);
            ////    WriteFileFromReturns(Date4, Returns4, minKapital4, weightKoef4, "LKOH+MIX_Rez.txt");
            //FirstDates.Add(Date4[0]);
            //LastDates.Add(Date4[Date4.Count - 1]);
            //for (int i = 0; i < Date4.Count; i++) { CommonDates.Add(Date4[i]); }

            //// Стратегия 5 Ri+Si
            //int minKapital5 = 220000;
            //int weightKoef5 = p;
            //SumKapital = SumKapital + minKapital5 * weightKoef5;
            //List<int> Date5 = GetDateFromFile("Ri+Si.txt");
            //List<int> Returns5 = GetReturnsFromFile("Ri+Si.txt", weightKoef5);
            ////    WriteFileFromReturns(Date5, Returns5, minKapital5, weightKoef5, "Ri+Si_Rez.txt");
            //FirstDates.Add(Date5[0]);
            //LastDates.Add(Date5[Date5.Count - 1]);
            //for (int i = 0; i < Date5.Count; i++) { CommonDates.Add(Date5[i]); }

            //// Стратегия 6   GAZR+VTB
            //int minKapital6 = 30000;
            //int weightKoef6 = q;
            //SumKapital = SumKapital + minKapital6 * weightKoef6;
            //List<int> Date6 = GetDateFromFile("GAZR+VTB.txt");
            //List<int> Returns6 = GetReturnsFromFile("GAZR+VTB.txt", weightKoef6);
            ////     WriteFileFromReturns(Date6, Returns6, minKapital6, weightKoef6, "GAZR+VTB_Rez.txt");
            //FirstDates.Add(Date6[0]);
            //LastDates.Add(Date6[Date6.Count - 1]);
            //for (int i = 0; i < Date6.Count; i++) { CommonDates.Add(Date6[i]); }

            //// Стратегия 7   Eu+Ri
            //int minKapital7 = 240000;
            //int weightKoef7 = s;
            //SumKapital = SumKapital + minKapital7 * weightKoef7;
            //List<int> Date7 = GetDateFromFile("Eu+Ri.txt");
            //List<int> Returns7 = GetReturnsFromFile("Eu+Ri.txt", weightKoef7);
            ////    WriteFileFromReturns(Date7, Returns7, minKapital7, weightKoef7, "Eu+Ri_Rez.txt");
            //FirstDates.Add(Date7[0]);
            //LastDates.Add(Date7[Date7.Count - 1]);
            //for (int i = 0; i < Date7.Count; i++) { CommonDates.Add(Date7[i]); }

        }

    }

}
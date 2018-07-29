using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RedMart_SpreadSheetCalculator
{
    public class Index
    {
        public int HorizontalX { get; set; }
        public int VerticalY { get; set; }
    }

    public class Cell
    {
        public decimal Value { get; set; }
        public bool IsProcessed { get; set; }
        public string CellContent { get; set; }

        public Cell(string cellContent)
        {
            this.CellContent = cellContent;
        }

        public override string ToString()
        {
            return $"CellContent is {CellContent} and Value is {Value}\n";
        }
    }

    public class Helpers
    {
        public static bool IsValidNumber(string s)
        {
            return Decimal.TryParse(s, out decimal n);
        }

        public static Index GetIndexOfCell(string s)
        {
            try
            {
                var charArr = s.ToCharArray();
                int _horizontal = (int)charArr[0] % 65; //ASCII of A is 65
                int _vertical = int.Parse(s.Substring(1, s.Length - 1)) - 1; //Subtract by 1 in the last for getting 0 index values for array
                return new Index { HorizontalX = _horizontal, VerticalY = _vertical };
            }
            catch (Exception ex)
            {
                throw new Exception($"Data format invalid - {ex.Message}");
            }
        }
    }

    public class Spreadsheet
    {
        private Cell[,] _sheetCells;
        private int _sizeHorizontal;
        private int _sizeVertical;

        private void InitializeSpreadSheetAndDimensions(string line)
        {
            var firstLineValues = line.Split(new[] {" "}, StringSplitOptions.None);

            if (firstLineValues.Length != 2)
                throw new Exception("Invalid cell sizes !");
           
            int[] size = new int[2];
            for (int i = 0; i < firstLineValues.Length; i++)
                size[i] = int.Parse(firstLineValues[i]);
            _sheetCells = new Cell[size[1], size[0]];
            _sizeVertical = size[0];
            _sizeHorizontal = size[1];
        }

        private void InitializeSpredSheetValues(string[] lines)
        {
            int _rndex = 0, _cIndex = 0, _cellCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                _sheetCells[_rndex, _cIndex++] = new Cell(lines[i]);
                _cellCount++;

                if (_cIndex == _sizeVertical)
                {
                    ++_rndex;
                    _cIndex = 0;
                }
            }

            if (_cellCount != _sizeHorizontal * _sizeVertical)
                throw new Exception("The input doesn't match number of cells !");
        }

        private decimal EvaluateCellValue(Cell currentCell, List<Cell> orderExecution)
        {
            if(!currentCell.IsProcessed)
            {
                if (orderExecution == null)
                    orderExecution = new List<Cell>();

                if(orderExecution.Contains(currentCell))
                {
                    StringBuilder errorContents = new StringBuilder();
                    foreach (var cellItem in orderExecution)
                        errorContents.Append(cellItem.ToString());

                    var errorStr = errorContents.ToString();

                    throw new Exception($"Cyclic dependency detected - \n{errorStr}\n");
                }


                orderExecution.Add(currentCell);

                var lineValues = currentCell.CellContent.Split(new[] {" "}, StringSplitOptions.None);
                var operands = new Stack<decimal>();
                for (int i = 0; i < lineValues.Length; i++)
                {
                    if (Helpers.IsValidNumber(lineValues[i]))
                        operands.Push(Decimal.Parse(lineValues[i]));
                    else
                    {
                        switch (lineValues[i])
                        {
                            case "+":
                                operands.Push(operands.Pop() + operands.Pop());
                                break;
                            case "*":
                                operands.Push(operands.Pop() * operands.Pop());
                                break;
                            case "/":
                                decimal divisor = operands.Pop();
                                operands.Push(operands.Pop() / divisor);
                                break;
                            case "-":
                                decimal subtractor = operands.Pop();
                                operands.Push(operands.Pop() - subtractor);
                                break;
                            default:
                                var index = Helpers.GetIndexOfCell(lineValues[i]);
                                operands.Push(EvaluateCellValue(_sheetCells[index.HorizontalX, index.VerticalY], orderExecution));
                                break;
                        }
                    }
                }

                currentCell.Value = operands.Pop();
                currentCell.IsProcessed = true;
            }

            return currentCell.Value;
        }

        public void PopulateSpreadSheet(string[] arguments)
        {	
            try
            {
                //Read all lines into an array from file or arguments
                var lines = arguments;

                if(lines == null || lines.Length == 0)
                    lines = File.ReadAllLines("input.txt");
                if (lines.Length < 1)
                {
                    throw new Exception("Input is not having values. Please feed values !");
                }

                //Step 1 : Initialize spread sheet and its dimensions with first value from input
                InitializeSpreadSheetAndDimensions(lines[0]);

                //Step 2: Initialize cells with remaining values from input
                InitializeSpredSheetValues(lines.Skip(1).ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invalid value(s) {ex.Message}");
                Console.WriteLine();
                Console.WriteLine($"Stack trace - {ex.ToString()}");
            }
        }

        public void EvaluteCellsInSpreadSheet()
        {
            for (int i = 0; i < _sizeHorizontal; i++)
            {
                for (int j = 0; j < _sizeVertical; j++)
                {
                    EvaluateCellValue(_sheetCells[i, j], null);
                }
            }
        }

        public void PrintSpreadSheet()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"{_sizeVertical} {_sizeHorizontal}\n");

            for (int i = 0; i < _sizeHorizontal; i++)
            {
                for (int j = 0; j < _sizeVertical; j++)
                {
                    builder.Append($"{_sheetCells[i, j].Value.ToString("#.00000")}\n");
                }
            }

            var output = builder.ToString();
            File.WriteAllText("output.txt", output);
            Console.Write(output);
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("------------");
            try
            {
                Spreadsheet spreadSheet = new Spreadsheet();
                spreadSheet.PopulateSpreadSheet(args);
                spreadSheet.EvaluteCellsInSpreadSheet();
                spreadSheet.PrintSpreadSheet();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("------------");
            Console.Read();
        }
    }
}

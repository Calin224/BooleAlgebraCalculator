using System.Data;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using NCalc;
using Expression = NCalc.Expression;

namespace BooleResolver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var expressionTb = expressionTextBox;
            expressionTb.FontSize = 18;
        }

        private List<Dictionary<string, bool>> GenerateTruthTable(List<string> variables)
        {
            int rows = (int)Math.Pow(2, variables.Count);
            var truthTable = new List<Dictionary<string, bool>>();

            for (int i = 0; i < rows; i++)
            {
                var row = new Dictionary<string, bool>();
                for (int j = 0; j < variables.Count; j++)
                {
                    row[variables[j]] = (i & (1 << j)) != 0;
                }

                truthTable.Add(row);
            }

            return truthTable;
        }

        private bool Evaluate(string expression, Dictionary<string, bool> values)
        {
            expression = ReplaceImplication(expression);
            expression = ReplaceXor(expression);
            Expression e = new Expression(expression);

            foreach (var pair in values)
            {
                e.Parameters[pair.Key] = pair.Value;
            }

            var result = e.Evaluate();
            if (result is bool booleanResult)
            {
                return booleanResult;
            }

            return false;
        }
        
        private string ReplaceXor(string expression)
        {
            while (expression.Contains("⊻"))
            {
                int operatorIndex = expression.IndexOf("⊻");
                
                string leftPart = expression.Substring(0, operatorIndex).TrimEnd();
                int leftParenCount = 0;
                int leftOpStart = leftPart.Length - 1;
                
                for (; leftOpStart >= 0; leftOpStart--)
                {
                    char c = leftPart[leftOpStart];
                    if (c == ')')
                        leftParenCount++;
                    else if (c == '(')
                        leftParenCount--;
                    
                    if (leftParenCount == 0 && (c == '(' || c == ' ' || c == '&' || c == '|' || c == '!' || c == '→'))
                        break;
                }
                
                string leftOperand = leftPart.Substring(leftOpStart + 1);
                string leftRemainder = leftOpStart >= 0 ? leftPart.Substring(0, leftOpStart + 1) : "";

                string rightPart = expression.Substring(operatorIndex + 1).TrimStart();
                int rightParenCount = 0;
                int rightOpEnd = 0;
                
                for (; rightOpEnd < rightPart.Length; rightOpEnd++)
                {
                    char c = rightPart[rightOpEnd];
                    if (c == '(')
                        rightParenCount++;
                    else if (c == ')')
                        rightParenCount--;
                    
                    if (rightParenCount == 0 && (c == ')' || c == ' ' || c == '&' || c == '|' || c == '⊻' || c == '→'))
                        break;
                }
                
                string rightOperand = rightPart.Substring(0, rightOpEnd);
                string rightRemainder = rightOpEnd < rightPart.Length ? rightPart.Substring(rightOpEnd) : "";

                string xorExpression = $"(!{leftOperand} && {rightOperand} || {leftOperand} && !{rightOperand})";
                expression = $"{leftRemainder}{xorExpression}{rightRemainder}";
            }

            return expression;
        }

        private string ReplaceImplication(string expression)
        {
            while (expression.Contains("→"))
            {
                int operatorIndex = expression.IndexOf("→");
                
                string leftPart = expression.Substring(0, operatorIndex).TrimEnd();
                int leftParenCount = 0;
                int leftOpStart = leftPart.Length - 1;
                
                for (; leftOpStart >= 0; leftOpStart--)
                {
                    char c = leftPart[leftOpStart];
                    if (c == ')')
                        leftParenCount++;
                    else if (c == '(')
                        leftParenCount--;
                    
                    if (leftParenCount == 0 && (c == '(' || c == ' ' || c == '&' || c == '|' || c == '!' || c == '⊻'))
                        break;
                }
                
                string leftOperand = leftPart.Substring(leftOpStart + 1);
                string leftRemainder = leftOpStart >= 0 ? leftPart.Substring(0, leftOpStart + 1) : "";
                
                string rightPart = expression.Substring(operatorIndex + 1).TrimStart();
                int rightParenCount = 0;
                int rightOpEnd = 0;
                
                for (; rightOpEnd < rightPart.Length; rightOpEnd++)
                {
                    char c = rightPart[rightOpEnd];
                    if (c == '(')
                        rightParenCount++;
                    else if (c == ')')
                        rightParenCount--;
                    
                    if (rightParenCount == 0 && (c == ')' || c == ' ' || c == '&' || c == '|' || c == '⊻' || c == '→'))
                        break;
                }
                
                string rightOperand = rightPart.Substring(0, rightOpEnd);
                string rightRemainder = rightOpEnd < rightPart.Length ? rightPart.Substring(rightOpEnd) : "";

                string implExpression = $"(!{leftOperand} || {rightOperand})";
                expression = $"{leftRemainder}{implExpression}{rightRemainder}";
            }

            return expression;
        }


        private void EvaluateExpressionButton_Click(object sender, RoutedEventArgs e)
        {
            string expression = expressionTextBox.Text;

            if (string.IsNullOrWhiteSpace(expression))
            {
                MessageBox.Show("The expression cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var variables = expression.Where(char.IsLetter).Distinct().Select(c => c.ToString()).OrderBy(c => c)
                .ToList();

            var truthTable = GenerateTruthTable(variables);

            var table = new DataTable();

            foreach (var variable in variables)
            {
                table.Columns.Add(variable, typeof(bool));
            }

            table.Columns.Add("Rezultat", typeof(bool));

            foreach (var row in truthTable)
            {
                var values = new List<object>();
                foreach (var variable in variables)
                {
                    values.Add(row[variable]);
                }

                bool result;
                try
                {
                    result = Evaluate(expression, row);
                }
                catch (NCalc.EvaluationException ex)
                {
                    MessageBox.Show($"Error evaluating expression: {ex.Message}", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                values.Add(result);

                table.Rows.Add(values.ToArray());
            }

            TruthTable.ItemsSource = table.DefaultView;
        }

        private void AddANDButton_Click(object sender, RoutedEventArgs e)
        {
            var expressionTb = expressionTextBox.Text;
            expressionTextBox.Text = expressionTb + " && ";
            expressionTextBox.Focus();
        }

        private void AddORButton_Click(object sender, RoutedEventArgs e)
        {
            var expressionTb = expressionTextBox.Text;
            expressionTextBox.Text = expressionTb + " || ";
            expressionTextBox.Focus();
        }

        private void AddNOTButton_Click(object sender, RoutedEventArgs e)
        {
            var expressionTb = expressionTextBox.Text;
            expressionTextBox.Text = expressionTb + " !";
            expressionTextBox.Focus();
        }

        private void AddXORButton_Click(object sender, RoutedEventArgs e)
        {
            var expressionTb = expressionTextBox.Text;
            expressionTextBox.Text = expressionTb + " \u22bb ";
            expressionTextBox.Focus();
        }

        private void AddIMPButton_Click(object sender, RoutedEventArgs e)
        {
            var expressionTb = expressionTextBox.Text;
            expressionTextBox.Text = expressionTb + " \u2192 ";
            expressionTextBox.Focus();
        }
    }
}
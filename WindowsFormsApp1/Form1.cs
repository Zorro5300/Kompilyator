using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.CodeDom.Compiler;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        // Переменные для отслеживания текущего файла и изменений
        private string currentFilePath = null;
        private bool isTextChanged = false;

        public Form1()
        {
            InitializeComponent();

            // Инициализация интерфейса
            InitializeInterface();
        }

        // КЛАСС ДЛЯ ХРАНЕНИЯ ИНФОРМАЦИИ ОБ ОШИБКЕ
        public class CompilerError
        {
            public int Line { get; set; }      // Номер строки (с 1)
            public int Column { get; set; }     // Номер столбца
            public string Message { get; set; } // Текст ошибки
        }

        private void InitializeInterface()
        {

            //outputRichTextBox.LinkClicked += OutputRichTextBox_LinkClicked;
            outputRichTextBox.MouseClick += OutputRichTextBox_MouseClick;

            // Настройка заголовка окна
            this.Text = "Текстовый редактор для языкового процессора";

            // Настройка области редактирования
            editorTextBox.TextChanged += EditorTextBox_TextChanged;
            editorTextBox.Dock = DockStyle.Fill;
            editorTextBox.ScrollBars = RichTextBoxScrollBars.Both;

            // Настройка области вывода результатов
            outputRichTextBox.ReadOnly = true;
            outputRichTextBox.Multiline = true;
            //outputRichTextBox.ScrollBars = ScrollBars.Both;
            outputRichTextBox.Dock = DockStyle.Fill;

            // Настройка сплитконтейнера
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Panel1MinSize = 200;
            splitContainer1.Panel2MinSize = 200;

            // Настройка меню и панели инструментов
            UpdateWindowTitle();
        }

        // ОБРАБОТКА КЛИКА ПО СООБЩЕНИЮ ОБ ОШИБКЕ
        private void OutputRichTextBox_MouseClick(object sender, MouseEventArgs e)
        {
            // Получаем позицию курсора в outputRichTextBox
            int charIndex = outputRichTextBox.GetCharIndexFromPosition(e.Location);
            int lineIndex = outputRichTextBox.GetLineFromCharIndex(charIndex);

            if (lineIndex >= 0 && lineIndex < outputRichTextBox.Lines.Length)
            {
                string line = outputRichTextBox.Lines[lineIndex];

                // Ищем строку с ошибкой в формате [строка X]
                if (line.Contains("[строка"))
                {
                    try
                    {
                        // Извлекаем все цифры из строки
                        string numbers = new string(line.Where(char.IsDigit).ToArray());
                        if (!string.IsNullOrEmpty(numbers))
                        {
                            int errorLine = int.Parse(numbers);

                            // Переходим к строке в редакторе
                            if (errorLine > 0 && errorLine <= editorTextBox.Lines.Length)
                            {
                                int charPos = editorTextBox.GetFirstCharIndexFromLine(errorLine - 1);
                                if (charPos >= 0)
                                {
                                    editorTextBox.SelectionStart = charPos;
                                    editorTextBox.SelectionLength = 0;
                                    editorTextBox.ScrollToCaret();
                                    editorTextBox.Focus();

                                    // Подсвечиваем строку желтым
                                    HighlightErrorLine(errorLine - 1);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        // ОБРАБОТКА КЛИКА ПО СООБЩЕНИЮ ОБ ОШИБКЕ
        /*private void OutputRichTextBox_MouseClick(object sender, MouseEventArgs e)
        {
            // Получаем позицию курсора в outputRichTextBox
            int charIndex = outputRichTextBox.GetCharIndexFromPosition(e.Location);
            outputRichTextBox.SelectionStart = charIndex;

            // Ищем строку с ошибкой (формат: "Ошибка [строка: X]")
            string line = outputRichTextBox.Lines[outputRichTextBox.GetLineFromCharIndex(charIndex)];

            if (line.Contains("строка:") || line.Contains("line:"))
            {
                try
                {
                    // Извлекаем номер строки
                    int lineNumber = 0;
                    foreach (string word in line.Split(' '))
                    {
                        if (word.Contains(":"))
                        {
                            string numStr = new string(word.Where(char.IsDigit).ToArray());
                            if (int.TryParse(numStr, out lineNumber))
                                break;
                        }
                    }

                    if (lineNumber > 0)
                    {
                        // Переходим к строке в редакторе
                        int index = editorTextBox.GetFirstCharIndexFromLine(lineNumber - 1);
                        if (index >= 0)
                        {
                            editorTextBox.SelectionStart = index;
                            editorTextBox.SelectionLength = 0;
                            editorTextBox.ScrollToCaret();
                            editorTextBox.Focus();
                        }
                    }
                }
                catch { }
            }
        }*/

        // Подсветка ошибочной строки в редакторе
        private void HighlightErrorLine(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= editorTextBox.Lines.Length)
                return;

            int startPos = editorTextBox.GetFirstCharIndexFromLine(lineNumber);
            int length = editorTextBox.Lines[lineNumber].Length;

            if (startPos >= 0 && length > 0)
            {
                // Запоминаем текущее выделение
                int oldStart = editorTextBox.SelectionStart;
                int oldLength = editorTextBox.SelectionLength;

                // Подсвечиваем строку желтым
                editorTextBox.SelectionStart = startPos;
                editorTextBox.SelectionLength = length;
                editorTextBox.SelectionBackColor = Color.LightYellow;

                // Таймер для сброса подсветки через 1 секунду
                Timer timer = new Timer();
                timer.Interval = 1000;
                timer.Tick += (s, args) =>
                {
                    editorTextBox.SelectionStart = startPos;
                    editorTextBox.SelectionLength = length;
                    editorTextBox.SelectionBackColor = Color.White;

                    // Восстанавливаем исходное выделение
                    editorTextBox.SelectionStart = oldStart;
                    editorTextBox.SelectionLength = oldLength;

                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        private void EditorTextBox_TextChanged(object sender, EventArgs e)
        {
            isTextChanged = true;
            UpdateWindowTitle();

            HighlightSyntax(); // Подсветка синтаксиса
        }


        private void создатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateNewFile();
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFile();
        }

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileAs();
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        // менюю ПРАВКА

        private void отменитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (editorTextBox.CanUndo)
                editorTextBox.Undo();
        }

        private void повторитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (editorTextBox.CanRedo)
                editorTextBox.Redo();
        }

        private void вырезатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editorTextBox.Cut();
        }

        private void копироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editorTextBox.Copy();
        }

        private void вставитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editorTextBox.Paste();
        }

        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(editorTextBox.SelectedText))
                editorTextBox.SelectedText = "";
        }

        private void выделитьВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            editorTextBox.SelectAll();
        }

        private void пускToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Очищаем область вывода
            outputRichTextBox.Clear();

            // Получаем текст из редактора
            string sourceCode = editorTextBox.Text;

            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                outputRichTextBox.Text = "Нет текста для анализа.";
                return;
            }

            // Запускаем анализ ошибок
            List<CompilerError> errors = AnalyzeCode(sourceCode);

            // Выводим результаты
            if (errors.Count == 0)
            {
                outputRichTextBox.SelectionColor = Color.Green;
                outputRichTextBox.AppendText("✓ Ошибок не найдено!\n");
                outputRichTextBox.AppendText($"✓ Проанализировано строк: {sourceCode.Split('\n').Length}\n");
            }
            else
            {
                outputRichTextBox.SelectionColor = Color.Red;
                outputRichTextBox.AppendText($"✗ Найдено ошибок: {errors.Count}\n");
                outputRichTextBox.SelectionColor = Color.Gray;
                outputRichTextBox.AppendText("═══════════════════════════════════════\n");

                foreach (var error in errors)
                {
                    // Ошибка с номером строки - красным
                    outputRichTextBox.SelectionColor = Color.DarkRed;
                    outputRichTextBox.AppendText($"[строка {error.Line}] ");

                    // Текст ошибки - черным
                    outputRichTextBox.SelectionColor = Color.Black;
                    outputRichTextBox.AppendText($"{error.Message}\n");
                }
            }
        }

        /*private void пускToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Теперь выводим примеры ошибок с номерами строк
            outputRichTextBox.Clear();

            // Простой "анализатор" - проверяем скобки
            string text = editorTextBox.Text;
            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("{"))
                {
                    outputRichTextBox.AppendText($"Ошибка в строке {i + 1}: Открывающая скобка\n");
                }
                if (lines[i].Contains("}"))
                {
                    outputRichTextBox.AppendText($"Ошибка в строке {i + 1}: Закрывающая скобка\n");
                }
            }

            if (outputRichTextBox.Text == "")
            {
                outputRichTextBox.Text = $"Текст для анализа ({editorTextBox.TextLength} символов):\n";
                outputRichTextBox.AppendText(editorTextBox.Text);
            }
        }*/

        private void вызовСправкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowHelp();
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowAbout();
        }

        // Обработчики для кнопок панели инструментов (свяжите их в дизайнере)
        private void toolStripButtonNew_Click(object sender, EventArgs e)
        {
            CreateNewFile();
        }

        private void toolStripButtonOpen_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            SaveFile();
        }

        private void toolStripButtonUndo_Click(object sender, EventArgs e)
        {
            отменитьToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonRedo_Click(object sender, EventArgs e)
        {
            повторитьToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonCut_Click(object sender, EventArgs e)
        {
            вырезатьToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonCopy_Click(object sender, EventArgs e)
        {
            копироватьToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonPaste_Click(object sender, EventArgs e)
        {
            вставитьToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonRun_Click(object sender, EventArgs e)
        {
            пускToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonHelp_Click(object sender, EventArgs e)
        {
            ShowHelp();
        }

        private void toolStripButtonAbout_Click(object sender, EventArgs e)
        {
            ShowAbout();
        }

        // Создание нового файла
        private void CreateNewFile()
        {
            if (CheckUnsavedChanges())
            {
                editorTextBox.Clear();
                currentFilePath = null;
                isTextChanged = false;
                UpdateWindowTitle();
                outputRichTextBox.Clear();
            }
        }

        // Открытие файла
        private void OpenFile()
        {
            if (!CheckUnsavedChanges())
                return;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = openFileDialog.FileName;
                        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                        editorTextBox.Text = fileContent;
                        currentFilePath = filePath;
                        isTextChanged = false;
                        UpdateWindowTitle();

                        outputRichTextBox.Text = $"Файл открыт: {Path.GetFileName(filePath)}\n";
                        outputRichTextBox.AppendText($"Размер: {fileContent.Length} символов\n");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии файла: {ex.Message}",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Сохранение файла
        private void SaveFile()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveFileAs();
            }
            else
            {
                SaveToFile(currentFilePath);
            }
        }

        // Сохранение файла как...
        private void SaveFileAs()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;
                    SaveToFile(filePath);
                }
            }
        }

        // Вспомогательный метод для сохранения в файл
        private void SaveToFile(string filePath)
        {
            try
            {
                File.WriteAllText(filePath, editorTextBox.Text, Encoding.UTF8);
                currentFilePath = filePath;
                isTextChanged = false;
                UpdateWindowTitle();

                outputRichTextBox.Text = $"Файл сохранен: {Path.GetFileName(filePath)}\n";
                outputRichTextBox.AppendText($"Время: {DateTime.Now:HH:mm:ss}\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Проверка на несохраненные изменения
        private bool CheckUnsavedChanges()
        {
            if (isTextChanged)
            {
                DialogResult result = MessageBox.Show(
                    "Сохранить изменения в текущем файле?",
                    "Несохраненные изменения",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveFile();
                }
                else if (result == DialogResult.Cancel)
                {
                    return false;
                }
            }
            return true;
        }

        // Показ справки
        private void ShowHelp()
        {
            string helpText = @"=== СПРАВКА ПО ТЕКСТОВОМУ РЕДАКТОРУ ===

Функции меню 'Файл':
• Создать - создание нового документа
• Открыть - открытие существующего файла
• Сохранить - сохранение текущего файла
• Сохранить как - сохранение под новым именем
• Выход - закрытие программы

Функции меню 'Правка':
• Отменить - отмена последнего действия
• Повторить - повтор отмененного действия
• Вырезать - перемещение выделенного текста в буфер
• Копировать - копирование выделенного текста
• Вставить - вставка текста из буфера
• Удалить - удаление выделенного текста
• Выделить все - выделение всего текста

Функции меню 'Справка':
• Вызов справки - открытие этого окна
• О программе - информация о приложении

Области интерфейса:
• Левая панель - редактор текста
• Правая панель - вывод результатов анализа
• Разделитель можно перемещать для изменения размеров областей";

            MessageBox.Show(helpText, "Справка", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Показ информации о программе
        private void ShowAbout()
        {
            string aboutText = @"Текстовый редактор для языкового процессора
Версия 1.0

Разработано в рамках курсового проекта.
Приложение предназначено для создания и редактирования
текстовых файлов с последующим анализом кода.

© 2024 Все права защищены.";

            MessageBox.Show(aboutText, "О программе",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Обновление заголовка окна
        private void UpdateWindowTitle()
        {
            string fileName = string.IsNullOrEmpty(currentFilePath) ?
                "Новый файл" : Path.GetFileName(currentFilePath);
            string modified = isTextChanged ? "*" : "";

            this.Text = $"Текстовый редактор - {fileName}{modified}";
        }


        private void HighlightSyntax() // подстветка для синтекса
        {
            // Сохранение текуей позиции мыши
            int selectionStart = editorTextBox.SelectionStart;
            int selectionLength = editorTextBox.SelectionLength;

            // отключения события дабы небыло бесконечного цикла
            editorTextBox.TextChanged -= EditorTextBox_TextChanged;

            string[] keywords = {
        "if", "else", "for", "while", "do", "switch", "case",
        "int", "string", "float", "double", "char", "bool", "void",
        "class", "public", "private", "protected", "static", "return"
    };

            // Сброс цвет всего текста на черный
            editorTextBox.SelectAll();
            editorTextBox.SelectionColor = Color.Black;
            editorTextBox.SelectionFont = new Font(editorTextBox.Font, FontStyle.Regular);

            // Подсвечиваем каждое ключевое слово
            foreach (string keyword in keywords)
            {
                int index = 0;
                while (index < editorTextBox.TextLength)
                {                   
                    int wordStart = editorTextBox.Find(keyword, index, RichTextBoxFinds.WholeWord);
                    if (wordStart == -1) break;

                    editorTextBox.SelectionStart = wordStart;
                    editorTextBox.SelectionLength = keyword.Length;
                    editorTextBox.SelectionColor = Color.Blue;
                    editorTextBox.SelectionFont = new Font(editorTextBox.Font, FontStyle.Bold);

                    index = wordStart + keyword.Length;
                }
            }

            // Восстанавливаем позицию мыши
            editorTextBox.SelectionStart = selectionStart;
            editorTextBox.SelectionLength = selectionLength;
            editorTextBox.SelectionColor = Color.Black;

            // Включаем событие обратно
            editorTextBox.TextChanged += EditorTextBox_TextChanged;
        }

        // АНАЛИЗ КОДА НА ОШИБКИ
        private List<CompilerError> AnalyzeCode(string sourceCode)
        {
            var errors = new List<CompilerError>();
            string[] lines = sourceCode.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int lineNumber = i + 1;
                string trimmedLine = line.Trim();

                // Пропускаем пустые строки и комментарии
                if (string.IsNullOrWhiteSpace(line) || trimmedLine.StartsWith("//"))
                    continue;

                // ======== ПРОВЕРКА 1: ОТСУТСТВИЕ ТОЧКИ С ЗАПЯТОЙ ========
                bool needsSemicolon = true;

                // Строки, которые НЕ должны заканчиваться точкой с запятой
                if (trimmedLine.EndsWith("{") ||
                    trimmedLine.EndsWith("}") ||
                    trimmedLine.StartsWith("if") ||
                    trimmedLine.StartsWith("else") ||
                    trimmedLine.StartsWith("for") ||
                    trimmedLine.StartsWith("while") ||
                    trimmedLine.StartsWith("do") ||
                    trimmedLine.StartsWith("switch") ||
                    trimmedLine.StartsWith("#"))
                {
                    needsSemicolon = false;
                }

                // Проверяем, нужна ли точка с запятой и есть ли она
                if (needsSemicolon && !trimmedLine.EndsWith(";"))
                {
                    // Дополнительная проверка: может это объявление функции?
                    if (!trimmedLine.Contains("(") || !trimmedLine.Contains(")"))
                    {
                        errors.Add(new CompilerError
                        {
                            Line = lineNumber,
                            Column = line.Length,
                            Message = "Отсутствует точка с запятой ';' в конце строки"
                        });
                    }
                }

                // ======== ПРОВЕРКА 2: НЕЗАКРЫТЫЕ СКОБКИ ========
                int openBrackets = line.Count(c => c == '(');
                int closeBrackets = line.Count(c => c == ')');

                if (openBrackets != closeBrackets)
                {
                    errors.Add(new CompilerError
                    {
                        Line = lineNumber,
                        Column = line.IndexOf('(') + 1,
                        Message = $"Несоответствие круглых скобок: открыто ({openBrackets}), закрыто ({closeBrackets})"
                    });
                }

                int openBraces = line.Count(c => c == '{');
                int closeBraces = line.Count(c => c == '}');

                if (openBraces != closeBraces)
                {
                    errors.Add(new CompilerError
                    {
                        Line = lineNumber,
                        Column = line.IndexOf('{') + 1,
                        Message = $"Несоответствие фигурных скобок: открыто ({openBraces}), закрыто ({closeBraces})"
                    });
                }

                // ======== ПРОВЕРКА 3: ПРОПУЩЕННЫЕ ЗАПЯТЫЕ ========
                // Проверяем объявления переменных
                if (line.Contains("int ") || line.Contains("string ") || line.Contains("var "))
                {
                    string[] parts = line.Split('=');
                    if (parts.Length > 1)
                    {
                        // Есть присваивание, проверяем правую часть
                        string rightPart = parts[1];
                        if (rightPart.Contains(",") && !rightPart.Contains("\""))
                        {
                            // Проверяем, что запятые не в кавычках
                            // Это сложно, пока пропустим
                        }
                    }
                }

                // Проверка на множественное объявление без запятых
                if ((line.Contains("int ") || line.Contains("string ")) && line.Contains(","))
                {
                    // Есть запятые - ок
                }
                else if ((line.Contains("int ") || line.Contains("string ")) && line.Contains("="))
                {
                    // Присваивание - ок
                }

                // ======== ПРОВЕРКА 4: ОПЕРАТОРЫ БЕЗ ОПЕРАНДОВ ========
                if (line.Contains("==") || line.Contains("!=") || line.Contains("<=") || line.Contains(">="))
                {
                    // Проверяем, что слева и справа что-то есть
                    int pos = line.IndexOf("==");
                    if (pos > 0 && pos < line.Length - 2)
                    {
                        // Есть символы слева и справа - ок
                    }
                    else
                    {
                        errors.Add(new CompilerError
                        {
                            Line = lineNumber,
                            Column = pos + 1,
                            Message = "Оператор сравнения без операндов"
                        });
                    }
                }
            }

            return errors;
        }

        // Обработка закрытия формы
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!CheckUnsavedChanges())
            {
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void editorTextBox_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e) // Создать файл
        {
            CreateNewFile();
        }

        private void pictureBox2_Click(object sender, EventArgs e) // Открыть папку
        {
            OpenFile();
        }

        private void pictureBox3_Click(object sender, EventArgs e) //Сохранить
        {
            SaveFile();
        }

        private void pictureBox4_Click(object sender, EventArgs e) //Назад
        {
            if (editorTextBox.CanUndo)
                editorTextBox.Undo();
        }

        private void pictureBox5_Click(object sender, EventArgs e) //Вперед
        {
            if (editorTextBox.CanRedo)
                editorTextBox.Redo();
        }

        private void pictureBox6_Click(object sender, EventArgs e) //Копировать
        {
            editorTextBox.Copy();
        }

        private void pictureBox7_Click(object sender, EventArgs e) //Вырезать
        {
            editorTextBox.Cut();
        }
    }
}
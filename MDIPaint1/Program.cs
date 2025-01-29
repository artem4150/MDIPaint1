using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking; // Подключаем DockPanel Suite (NuGet: DockPanelSuite)
// Подключите сборку RibbonWinForms (или другой Ribbon), если планируете его использовать
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MDI_DrawApp_DockPanel
{
    static class Program
    {
        /// <summary>
        /// Точка входа в приложение.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Создаём и запускаем главное окно
            Application.Run(new MainForm());
        }
    }

    /// <summary>
    /// Перечисление доступных инструментов рисования
    /// </summary>
    public enum DrawingTool
    {
        None,
        Pen,
        Line,
        Ellipse,
        Eraser,
        Bucket,          // Заливка
        Text,
        Polygon,
        // Дополнительно
        ZoomIn,
        ZoomOut
    }

    /// <summary>
    /// Форма "О программе"
    /// </summary>
    public class AboutForm : Form
    {
        public AboutForm()
        {
            this.Text = "О программе";
            this.Size = new Size(400, 200);
            var label = new Label();
            label.Text = "MDI-приложение для рисования.\n" +
                        "Поддержка BMP, JPG, PNG.\n" +
                        "Все права не защищены :)";
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(label);
        }
    }

    /// <summary>
    /// Основная форма MDI-приложения c лентой и DockPanel
    /// </summary>
    public partial class MainForm : Form
    {
        // DockPanel из DockPanelSuite3
        private DockPanel dockPanel;
        //private WeifenLuo.WinFormsUI.Docking.VisualStudioToolStripExtender vsExtender;

        // Лента (Ribbon) - используем любой доступный компонент ribbon
        // Ниже пример с гипотетическим RibbonControl (его может не быть в стандартном .NET)
        // Если используете ToolStrip + MenuStrip, логику аналогично можно перенести в них.
        private MenuStrip mainMenu;
        private ToolStrip toolStrip;

        // Отслеживание активного инструмента, цвета, толщины, заполнения
        public DrawingTool CurrentTool { get; set; } = DrawingTool.None;
        public Color CurrentColor { get; set; } = Color.Black;
        public float CurrentThickness { get; set; } = 2f;
        public bool FillShapes { get; set; } = false;   // Для эллипса, многоугольника

        // Параметр для правильного многоугольника (n-угольник)
        public int PolygonSides { get; set; } = 5;

        public MainForm()
        {
            IsMdiContainer = true; // классический MDI, но далее используем DockPanel
            this.Text = "MDI Редактор изображений";
            this.WindowState = FormWindowState.Maximized;

            InitializeDockPanel();
            InitializeMenuAndToolbars();
        }

        private void InitializeDockPanel()
        {
            dockPanel = new DockPanel();
            dockPanel.Dock = DockStyle.Fill;

            // Можно настроить стиль документа, чтобы вкладки были видны
            dockPanel.DocumentStyle = DocumentStyle.DockingMdi;
            this.Controls.Add(dockPanel);

            // Extender для совместимости со стилями
            //vsExtender = new VisualStudioToolStripExtender();
            //vsExtender.SetStyle(this.mainMenu, VisualStudioToolStripExtender.VsVersion.Vs2015, null);
        }

        private void InitializeMenuAndToolbars()
        {
            // Вариант: используем MenuStrip + ToolStrip в качестве "Ribbon"-заменителя
            // Для настоящего Ribbon нужны дополнительные библиотеки.
            mainMenu = new MenuStrip();
            toolStrip = new ToolStrip();

            // Меню "Файл"
            var fileMenu = new ToolStripMenuItem("Файл");
            var newMenuItem = new ToolStripMenuItem("Новый", null, OnNewFile) { ShortcutKeys = Keys.Control | Keys.N };
            var openMenuItem = new ToolStripMenuItem("Открыть…", null, OnOpenFile) { ShortcutKeys = Keys.Control | Keys.O };
            var saveMenuItem = new ToolStripMenuItem("Сохранить", null, OnSaveFile) { ShortcutKeys = Keys.Control | Keys.S };
            var saveAsMenuItem = new ToolStripMenuItem("Сохранить как…", null, OnSaveAsFile);
            var exitMenuItem = new ToolStripMenuItem("Выход", null, OnExit);

            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                newMenuItem, openMenuItem, new ToolStripSeparator(),
                saveMenuItem, saveAsMenuItem, new ToolStripSeparator(),
                exitMenuItem
            });

            // Меню "Правка" (цвет, толщина, заполнение)
            var editMenu = new ToolStripMenuItem("Правка");
            var colorMenuItem = new ToolStripMenuItem("Цвет пера…", null, OnChangeColor);
            var thicknessMenuItem = new ToolStripMenuItem("Толщина пера…", null, OnChangeThickness);
            var fillMenuItem = new ToolStripMenuItem("Закрашивать фигуры?", null, OnToggleFill) { CheckOnClick = true };
            editMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                colorMenuItem, thicknessMenuItem, fillMenuItem
            });

            // Меню "Инструменты" (выбор инструмента)
            var toolsMenu = new ToolStripMenuItem("Инструменты");
            var penMenuItem = new ToolStripMenuItem("Перо", null, (s, e) => SelectTool(DrawingTool.Pen));
            var lineMenuItem = new ToolStripMenuItem("Линия", null, (s, e) => SelectTool(DrawingTool.Line));
            var ellipseMenuItem = new ToolStripMenuItem("Эллипс", null, (s, e) => SelectTool(DrawingTool.Ellipse));
            var eraserMenuItem = new ToolStripMenuItem("Ластик", null, (s, e) => SelectTool(DrawingTool.Eraser));
            var bucketMenuItem = new ToolStripMenuItem("Ведро", null, (s, e) => SelectTool(DrawingTool.Bucket));
            var textMenuItem = new ToolStripMenuItem("Текст", null, (s, e) => SelectTool(DrawingTool.Text));
            var polyMenuItem = new ToolStripMenuItem("Прав. многоугольник (n-угольник)", null, OnPolygonSettings);
            var zoomInMenuItem = new ToolStripMenuItem("Масштаб +", null, (s, e) => SelectTool(DrawingTool.ZoomIn));
            var zoomOutMenuItem = new ToolStripMenuItem("Масштаб -", null, (s, e) => SelectTool(DrawingTool.ZoomOut));

            toolsMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                penMenuItem, lineMenuItem, ellipseMenuItem, eraserMenuItem,
                bucketMenuItem, textMenuItem, polyMenuItem,
                new ToolStripSeparator(),
                zoomInMenuItem, zoomOutMenuItem
            });

            // Меню "Окна" (авторасположение: каскад, горизонтально, вертикально и т.д.)
            var windowMenu = new ToolStripMenuItem("Окна");
            var cascadeItem = new ToolStripMenuItem("Каскадом", null, (s, e) => this.LayoutMdi(MdiLayout.Cascade));
            var tileHItem = new ToolStripMenuItem("Сверху вниз", null, (s, e) => this.LayoutMdi(MdiLayout.TileHorizontal));
            var tileVItem = new ToolStripMenuItem("Слева направо", null, (s, e) => this.LayoutMdi(MdiLayout.TileVertical));
            windowMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                cascadeItem, tileHItem, tileVItem
            });

            // Меню "Справка"
            var helpMenu = new ToolStripMenuItem("Справка");
            var aboutItem = new ToolStripMenuItem("О программе", null, OnAbout);
            helpMenu.DropDownItems.Add(aboutItem);

            mainMenu.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, toolsMenu, windowMenu, helpMenu });
            this.MainMenuStrip = mainMenu;
            this.Controls.Add(mainMenu);

            // Инструментальная панель (упрощённо)
            toolStrip.Items.Add(new ToolStripButton("New", null, OnNewFile) { ToolTipText = "Новый (Ctrl+N)" });
            toolStrip.Items.Add(new ToolStripButton("Open", null, OnOpenFile) { ToolTipText = "Открыть (Ctrl+O)" });
            toolStrip.Items.Add(new ToolStripButton("Save", null, OnSaveFile) { ToolTipText = "Сохранить (Ctrl+S)" });
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Pen", null, (s, e) => SelectTool(DrawingTool.Pen)) { ToolTipText = "Перо" });
            toolStrip.Items.Add(new ToolStripButton("Line", null, (s, e) => SelectTool(DrawingTool.Line)) { ToolTipText = "Линия" });
            toolStrip.Items.Add(new ToolStripButton("Ellipse", null, (s, e) => SelectTool(DrawingTool.Ellipse)) { ToolTipText = "Эллипс" });
            toolStrip.Items.Add(new ToolStripButton("Eraser", null, (s, e) => SelectTool(DrawingTool.Eraser)) { ToolTipText = "Ластик" });
            toolStrip.Items.Add(new ToolStripButton("Bucket", null, (s, e) => SelectTool(DrawingTool.Bucket)) { ToolTipText = "Заливка" });
            toolStrip.Items.Add(new ToolStripButton("Text", null, (s, e) => SelectTool(DrawingTool.Text)) { ToolTipText = "Текст" });
            toolStrip.Items.Add(new ToolStripButton("Polygon", null, OnPolygonSettings) { ToolTipText = "Правильный многоугольник" });
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Zoom +", null, (s, e) => SelectTool(DrawingTool.ZoomIn)) { ToolTipText = "Масштаб +" });
            toolStrip.Items.Add(new ToolStripButton("Zoom -", null, (s, e) => SelectTool(DrawingTool.ZoomOut)) { ToolTipText = "Масштаб -" });
            toolStrip.Items.Add(new ToolStripSeparator());
            ToolStripButton fillButton = new ToolStripButton(); // Сначала объявляем переменную

            fillButton.Text = "Fill OFF";
            fillButton.Click += (s, e) =>
            {
                FillShapes = !FillShapes;
                fillButton.Text = FillShapes ? "Fill ON" : "Fill OFF";
            };

            toolStrip.Items.Add(fillButton); // Теперь добавляем кнопку в панель инструментов

            // Присоединяем ToolStrip
            toolStrip.Dock = DockStyle.Top;
            this.Controls.Add(toolStrip);

            // Сразу после загрузки неактивны некоторые пункты, пока нет документа
            UpdateMenuItemsState(false);
        }

        private void OnAbout(object sender, EventArgs e)
        {
            var about = new AboutForm();
            about.ShowDialog();
        }

        private void OnNewFile(object sender, EventArgs e)
        {
            try
            {
                // Создать новый дочерний документ
                var doc = new ImageDocument(this);
                doc.Show(dockPanel, DockState.Document);
                UpdateMenuItemsState(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании нового документа:\n" + ex.Message);
            }
        }

        private void OnOpenFile(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Изображения (*.bmp;*.jpg;*.jpeg;*.png)|*.bmp;*.jpg;*.jpeg;*.png";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        var doc = new ImageDocument(this);
                        doc.OpenImage(ofd.FileName);
                        doc.Show(dockPanel, DockState.Document);
                        UpdateMenuItemsState(true);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии файла:\n" + ex.Message);
            }
        }

        private void OnSaveFile(object sender, EventArgs e)
        {
            var doc = dockPanel.ActiveDocument as ImageDocument;
            if (doc != null)
            {
                doc.SaveImage(false); // Сохранить без запроса имени, если уже есть имя
            }
        }

        private void OnSaveAsFile(object sender, EventArgs e)
        {
            var doc = dockPanel.ActiveDocument as ImageDocument;
            if (doc != null)
            {
                doc.SaveImage(true); // С запросом имени
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OnChangeColor(object sender, EventArgs e)
        {
            using (ColorDialog cd = new ColorDialog())
            {
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    CurrentColor = cd.Color;
                }
            }
        }

        private void OnChangeThickness(object sender, EventArgs e)
        {
            // Простое диалоговое окно ввода
            string input = Microsoft.VisualBasic.Interaction.InputBox("Введите толщину пера (float):",
                "Толщина пера", CurrentThickness.ToString());
            if (float.TryParse(input, out float value))
            {
                CurrentThickness = Math.Max(0.1f, value);
            }
        }

        private void OnToggleFill(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            FillShapes = item.Checked;
        }

        private void OnPolygonSettings(object sender, EventArgs e)
        {
            // Запросить число сторон
            string input = Microsoft.VisualBasic.Interaction.InputBox("Введите количество сторон n для многоугольника:",
                "Настройки многоугольника", PolygonSides.ToString());
            if (int.TryParse(input, out int n))
            {
                PolygonSides = Math.Max(3, n);
            }
            // Выбираем инструмент "Polygon"
            SelectTool(DrawingTool.Polygon);
        }

        private void SelectTool(DrawingTool tool)
        {
            this.CurrentTool = tool;
        }

        /// <summary>
        /// Активировать/деактивировать элементы "Сохранить" и т.д.
        /// </summary>
        public void UpdateMenuItemsState(bool hasDocument)
        {
            // Пробежимся по MenuStrip
            foreach (ToolStripMenuItem topItem in mainMenu.Items)
            {
                foreach (ToolStripItem subItem in topItem.DropDownItems)
                {
                    if (subItem.Text.StartsWith("Сохранить"))
                    {
                        subItem.Enabled = hasDocument;
                    }
                }
            }

            // Аналогично для toolStrip-кнопок
            foreach (ToolStripItem item in toolStrip.Items)
            {
                if (item.Text == "Save")
                {
                    item.Enabled = hasDocument;
                }
            }
        }
    }

    /// <summary>
    /// Дочерняя форма-документ для редактирования изображения
    /// Используем DockContent из DockPanelSuite
    /// </summary>
    public class ImageDocument : DockContent
    {
        private MainForm _mainForm;
        private Bitmap _bitmap;        // Текущее изображение
        private string _fileName;      // Имя файла (если уже сохранён/открыт)
        private bool _modified;        // Флаг изменения

        // Для рисования временной фигуры
        private bool _mouseDown = false;
        private Point _startPoint;
        private Point _currentPoint;

        // Вспомогательные для многоугольника (простой вариант — рисовать как эллипс, но по точкам)
        // Для «промежуточного» отрисовывания – пока ограничимся «одним щелчком»:
        // реализация может меняться по условию.

        // Масштаб
        private float _zoomFactor = 1.0f;

        public ImageDocument(MainForm mainForm)
        {
            _mainForm = mainForm;
            this.Text = "Новый рисунок";
            _fileName = null;
            _modified = false;
            // Создать пустую картинку (по умолчанию 800x600)
            _bitmap = new Bitmap(800, 600, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(_bitmap))
            {
                g.Clear(Color.White);
            }

            this.CloseButton = true;
            this.FormClosing += ImageDocument_FormClosing;

            // Включаем двойную буферизацию
            this.DoubleBuffered = true;

            // Подписываемся на события мыши
            this.MouseDown += ImageDocument_MouseDown;
            this.MouseMove += ImageDocument_MouseMove;
            this.MouseUp += ImageDocument_MouseUp;
            this.Paint += ImageDocument_Paint;
        }

        public void OpenImage(string path)
        {
            Bitmap bmp = (Bitmap)Image.FromFile(path);
            _bitmap?.Dispose();
            _bitmap = bmp;
            _fileName = path;
            this.Text = System.IO.Path.GetFileName(path);
            _modified = false;
        }

        public void SaveImage(bool saveAs)
        {
            // Если saveAs или нет имени файла, запрашиваем
            if (saveAs || string.IsNullOrEmpty(_fileName))
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "BMP Image (*.bmp)|*.bmp|JPEG Image (*.jpg)|*.jpg";
                    sfd.AddExtension = true;
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        _fileName = sfd.FileName;
                    }
                    else
                    {
                        return; // пользователь отменил
                    }
                }
            }

            // Сохраняем в выбранном формате
            if (!string.IsNullOrEmpty(_fileName))
            {
                try
                {
                    var ext = System.IO.Path.GetExtension(_fileName).ToLower();
                    if (ext == ".bmp")
                    {
                        _bitmap.Save(_fileName, System.Drawing.Imaging.ImageFormat.Bmp);
                    }
                    else
                    {
                        // По умолчанию - jpeg
                        _bitmap.Save(_fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                    this.Text = System.IO.Path.GetFileName(_fileName);
                    _modified = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении файла:\n" + ex.Message);
                }
            }
        }

        private void ImageDocument_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_modified)
            {
                // Предложить сохранить изменения
                var result = MessageBox.Show("Файл был изменён. Сохранить изменения?",
                    "Сохранение", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    SaveImage(false);
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true; // отменяем закрытие
                }
            }
        }

        private void ImageDocument_MouseDown(object sender, MouseEventArgs e)
        {
            _mouseDown = true;
            _startPoint = e.Location;
            _currentPoint = e.Location;
        }

        private void ImageDocument_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mouseDown)
            {
                _currentPoint = e.Location;
                // Для инструментов типа "Перо" рисуем сразу, для остальных — лишь показываем «рамку»
                if (_mainForm.CurrentTool == DrawingTool.Pen)
                {
                    using (Graphics g = Graphics.FromImage(_bitmap))
                    {
                        // Корректируем координаты с учётом зума
                        var pt1 = ScreenToBitmap(_currentPoint);
                        var pt0 = ScreenToBitmap(_startPoint);

                        using (Pen pen = new Pen(_mainForm.CurrentColor, _mainForm.CurrentThickness))
                        {
                            pen.StartCap = LineCap.Round;
                            pen.EndCap = LineCap.Round;
                            g.DrawLine(pen, pt0, pt1);
                        }
                    }
                    _startPoint = e.Location;
                    _modified = true;
                    Invalidate();
                }
                else if (_mainForm.CurrentTool == DrawingTool.Eraser)
                {
                    // Ластик — рисуем белым кружком
                    using (Graphics g = Graphics.FromImage(_bitmap))
                    {
                        var pt = ScreenToBitmap(e.Location);
                        float size = _mainForm.CurrentThickness * 5; // Радиус ластика
                        RectangleF eraserRect = new RectangleF(pt.X - size / 2, pt.Y - size / 2, size, size);
                        using (SolidBrush brush = new SolidBrush(Color.White))
                        {
                            g.FillEllipse(brush, eraserRect);
                        }
                    }
                    _startPoint = e.Location;
                    _modified = true;
                    Invalidate();
                }
                else
                {
                    // Просто визуально показываем "рамку"/"фигуру" пока пользователь удерживает мышь
                    Invalidate();
                }
            }
        }

        private void ImageDocument_MouseUp(object sender, MouseEventArgs e)
        {
            if (_mouseDown)
            {
                _mouseDown = false;
                _currentPoint = e.Location;

                // В зависимости от выбранного инструмента рисуем итоговую фигуру
                switch (_mainForm.CurrentTool)
                {
                    case DrawingTool.Line:
                        DrawLine(_startPoint, _currentPoint);
                        break;
                    case DrawingTool.Ellipse:
                        DrawEllipse(_startPoint, _currentPoint, _mainForm.FillShapes);
                        break;
                    case DrawingTool.Bucket:
                        // Заливка области
                        FloodFill(_currentPoint);
                        break;
                    case DrawingTool.Text:
                        PlaceText(_currentPoint);
                        break;
                    case DrawingTool.Polygon:
                        DrawPolygon(_startPoint, _currentPoint, _mainForm.PolygonSides, _mainForm.FillShapes);
                        break;
                    case DrawingTool.ZoomIn:
                        Zoom(true);
                        break;
                    case DrawingTool.ZoomOut:
                        Zoom(false);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ImageDocument_Paint(object sender, PaintEventArgs e)
        {
            // Отрисовка текущего _bitmap с учётом зума
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

            float scaledWidth = _bitmap.Width * _zoomFactor;
            float scaledHeight = _bitmap.Height * _zoomFactor;
            e.Graphics.DrawImage(_bitmap, 0, 0, scaledWidth, scaledHeight);

            // Если пользователь рисует "рамку" (Line, Ellipse, Polygon)
            if (_mouseDown)
            {
                switch (_mainForm.CurrentTool)
                {
                    case DrawingTool.Line:
                    case DrawingTool.Ellipse:
                    case DrawingTool.Polygon:
                        Point ptStart = _startPoint;
                        Point ptEnd = _currentPoint;
                        using (Pen pen = new Pen(Color.Red, 1))
                        {
                            pen.DashStyle = DashStyle.Dot;
                            Rectangle rect = GetNormalizedRectangle(ptStart, ptEnd);
                            e.Graphics.DrawRectangle(pen, rect);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void DrawLine(Point screenStart, Point screenEnd)
        {
            using (Graphics g = Graphics.FromImage(_bitmap))
            {
                var ptStart = ScreenToBitmap(screenStart);
                var ptEnd = ScreenToBitmap(screenEnd);
                using (Pen pen = new Pen(_mainForm.CurrentColor, _mainForm.CurrentThickness))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLine(pen, ptStart, ptEnd);
                }
            }
            _modified = true;
            Invalidate();
        }

        private void DrawEllipse(Point screenStart, Point screenEnd, bool filled)
        {
            using (Graphics g = Graphics.FromImage(_bitmap))
            {
                var ptStart = ScreenToBitmap(screenStart);
                var ptEnd = ScreenToBitmap(screenEnd);
                Rectangle rect = GetNormalizedRectangle(ptStart, ptEnd);

                if (filled)
                {
                    using (SolidBrush brush = new SolidBrush(_mainForm.CurrentColor))
                    {
                        g.FillEllipse(brush, rect);
                    }
                }
                else
                {
                    using (Pen pen = new Pen(_mainForm.CurrentColor, _mainForm.CurrentThickness))
                    {
                        g.DrawEllipse(pen, rect);
                    }
                }
            }
            _modified = true;
            Invalidate();
        }

        private void DrawPolygon(Point screenStart, Point screenEnd, int sides, bool filled)
        {
            // Построим правильный многоугольник (n-угольник), вписанный в прямоугольник
            // от screenStart до screenEnd
            var ptStart = ScreenToBitmap(screenStart);
            var ptEnd = ScreenToBitmap(screenEnd);
            Rectangle rect = GetNormalizedRectangle(ptStart, ptEnd);

            PointF center = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            float radius = Math.Min(rect.Width, rect.Height) / 2f;

            List<PointF> points = new List<PointF>();
            for (int i = 0; i < sides; i++)
            {
                double angle = 2.0 * Math.PI * i / sides - Math.PI / 2;
                float x = center.X + (float)(radius * Math.Cos(angle));
                float y = center.Y + (float)(radius * Math.Sin(angle));
                points.Add(new PointF(x, y));
            }

            using (Graphics g = Graphics.FromImage(_bitmap))
            {
                if (filled)
                {
                    using (SolidBrush brush = new SolidBrush(_mainForm.CurrentColor))
                    {
                        g.FillPolygon(brush, points.ToArray());
                    }
                }
                else
                {
                    using (Pen pen = new Pen(_mainForm.CurrentColor, _mainForm.CurrentThickness))
                    {
                        g.DrawPolygon(pen, points.ToArray());
                    }
                }
            }
            _modified = true;
            Invalidate();
        }

        private void FloodFill(Point screenPoint)
        {
            // Простейшая заливка «по пикселям» (Flood Fill). Нужно учесть зум.
            var bmpPoint = ScreenToBitmap(screenPoint);
            if (bmpPoint.X < 0 || bmpPoint.X >= _bitmap.Width ||
                bmpPoint.Y < 0 || bmpPoint.Y >= _bitmap.Height)
                return;

            Color targetColor = _bitmap.GetPixel(bmpPoint.X, bmpPoint.Y);
            if (targetColor.ToArgb() == _mainForm.CurrentColor.ToArgb()) return;

            // Выполним fill (BFS/DFS)
            Stack<Point> stack = new Stack<Point>();
            stack.Push(bmpPoint);

            while (stack.Count > 0)
            {
                var pt = stack.Pop();
                if (pt.X < 0 || pt.X >= _bitmap.Width ||
                    pt.Y < 0 || pt.Y >= _bitmap.Height)
                    continue;

                if (_bitmap.GetPixel(pt.X, pt.Y).ToArgb() == targetColor.ToArgb())
                {
                    _bitmap.SetPixel(pt.X, pt.Y, _mainForm.CurrentColor);
                    stack.Push(new Point(pt.X + 1, pt.Y));
                    stack.Push(new Point(pt.X - 1, pt.Y));
                    stack.Push(new Point(pt.X, pt.Y + 1));
                    stack.Push(new Point(pt.X, pt.Y - 1));
                }
            }

            _modified = true;
            Invalidate();
        }

        private void PlaceText(Point screenPoint)
        {
            // В простейшем случае сразу показываем диалог ввода текста
            string input = Microsoft.VisualBasic.Interaction.InputBox("Введите текст для вставки:",
                "Текст", "");
            if (!string.IsNullOrEmpty(input))
            {
                var bmpPoint = ScreenToBitmap(screenPoint);
                using (Graphics g = Graphics.FromImage(_bitmap))
                {
                    using (Font font = new Font("Arial", 16))
                    {
                        using (SolidBrush brush = new SolidBrush(_mainForm.CurrentColor))
                        {
                            g.DrawString(input, font, brush, bmpPoint);
                        }
                    }
                }
                _modified = true;
                Invalidate();
            }
        }

        private void Zoom(bool zoomIn)
        {
            // Увеличиваем или уменьшаем _zoomFactor
            float step = 0.2f;
            if (zoomIn)
            {
                _zoomFactor += step;
            }
            else
            {
                _zoomFactor -= step;
                if (_zoomFactor < 0.2f) _zoomFactor = 0.2f;
            }
            Invalidate();
        }

        /// <summary>
        /// Преобразовать экранные координаты (с учётом формы) в координаты внутри _bitmap
        /// </summary>
        private Point ScreenToBitmap(Point pt)
        {
            // С учётом зума
            float x = pt.X / _zoomFactor;
            float y = pt.Y / _zoomFactor;
            return new Point((int)x, (int)y);
        }

        /// <summary>
        /// Получить прямоугольник со старта по конец с нормализованными координатами
        /// </summary>
        private static Rectangle GetNormalizedRectangle(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int w = Math.Abs(p1.X - p2.X);
            int h = Math.Abs(p1.Y - p2.Y);
            return new Rectangle(x, y, w, h);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // Активное окно -> доступность команд в меню (Сохранить, Сохранить как и т.д.)
            _mainForm.UpdateMenuItemsState(true);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            // При потере фокуса, если нет других активных документов, отключаем
            if (this.DockPanel.ActiveDocument == null)
            {
                _mainForm.UpdateMenuItemsState(false);
            }
        }
    }
}

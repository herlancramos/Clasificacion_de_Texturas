using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class Form1 : Form
    {
        // Controles
        private Button buttonCargar;
        private Button buttonCesped;
        private Button buttonTierra;
        private Button buttonCemento;
        private Button buttonAsfalto;
        private PictureBox pictureBox1;
        private TextBox textBoxInfo;
        private Label labelResultado;   // ← NUEVO: muestra el resultado de la clasificación

        // Variables para procesamiento
        private Bitmap imagenOriginal;
        private int tamBloque = 20;      // tamaño de la ventana para analizar textura
        private double[,] texturas = new double[4, 6];
        private bool[] texturaAprendida = new bool[4];
        private string[] nombresTextura = { "Césped", "Tierra", "Cemento", "Asfalto" };
        private int texturaAprendiendo = -1;

        public Form1()
        {
            InicializarComponentesManual();
        }

        private void InicializarComponentesManual()
        {
            this.Text = "Clasificador de Texturas (por puntos)";
            this.Size = new Size(720, 550);
            this.StartPosition = FormStartPosition.CenterScreen;

            // PictureBox
            pictureBox1 = new PictureBox();
            pictureBox1.Location = new Point(280, 12);
            pictureBox1.Size = new Size(400, 400);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            pictureBox1.BackColor = Color.LightGray;
            pictureBox1.MouseClick += pictureBox1_MouseClick;

            // Botones
            buttonCargar = new Button { Text = "Cargar Imagen", Location = new Point(12, 12), Size = new Size(130, 30) };
            buttonCargar.Click += buttonCargar_Click;

            buttonCesped = new Button { Text = "Aprender Césped", Location = new Point(12, 50), Size = new Size(130, 30) };
            buttonCesped.Click += (s, e) => AprenderTextura(0);

            buttonTierra = new Button { Text = "Aprender Tierra", Location = new Point(12, 88), Size = new Size(130, 30) };
            buttonTierra.Click += (s, e) => AprenderTextura(1);

            buttonCemento = new Button { Text = "Aprender Cemento", Location = new Point(12, 126), Size = new Size(130, 30) };
            buttonCemento.Click += (s, e) => AprenderTextura(2);

            buttonAsfalto = new Button { Text = "Aprender Asfalto", Location = new Point(12, 164), Size = new Size(130, 30) };
            buttonAsfalto.Click += (s, e) => AprenderTextura(3);

            // Cuadro de información
            textBoxInfo = new TextBox
            {
                Location = new Point(12, 210),
                Size = new Size(250, 60),
                Multiline = true,
                ReadOnly = true,
                BackColor = SystemColors.ControlLight
            };
            textBoxInfo.Text = "Carga una imagen. Aprende las texturas haciendo clic en los botones y luego en la imagen. Después, haz clic en cualquier punto para clasificarlo.";

            // Label para mostrar el resultado de la clasificación
            labelResultado = new Label
            {
                Location = new Point(12, 290),
                Size = new Size(250, 50),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightYellow,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Resultado: --"
            };

            // Agregar controles
            this.Controls.Add(pictureBox1);
            this.Controls.Add(buttonCargar);
            this.Controls.Add(buttonCesped);
            this.Controls.Add(buttonTierra);
            this.Controls.Add(buttonCemento);
            this.Controls.Add(buttonAsfalto);
            this.Controls.Add(textBoxInfo);
            this.Controls.Add(labelResultado);
        }

        // Cargar imagen
        private void buttonCargar_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Imagenes|*.jpg;*.jpeg;*.png;*.bmp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                imagenOriginal = new Bitmap(ofd.FileName);
                pictureBox1.Image = (Bitmap)imagenOriginal.Clone();
                textBoxInfo.Text = "Imagen cargada. Ahora aprende texturas (elige un botón y haz clic en la zona correspondiente).";
                labelResultado.Text = "Resultado: --";
            }
        }

        // Preparar aprendizaje de una textura
        private void AprenderTextura(int indice)
        {
            if (imagenOriginal == null)
            {
                MessageBox.Show("Primero carga una imagen.");
                return;
            }
            texturaAprendiendo = indice;
            textBoxInfo.Text = $"Haz clic en la imagen sobre una zona de {nombresTextura[indice]} (ventana de {tamBloque}x{tamBloque})";
        }

        // Evento del clic en el PictureBox
        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (imagenOriginal == null) return;

            // Si estamos en modo aprendizaje, aprender la textura
            if (texturaAprendiendo >= 0)
            {
                AprenderTexturaDesdeClic(e.X, e.Y);
                return;
            }

            // Si NO estamos en modo aprendizaje, entonces clasificar el punto
            ClasificarPunto(e.X, e.Y);
        }

        // Aprende la textura del punto donde se hizo clic (ventana alrededor)
        private void AprenderTexturaDesdeClic(int x, int y)
        {
            int mitad = tamBloque / 2;
            int x0 = Math.Max(0, x - mitad);
            int y0 = Math.Max(0, y - mitad);
            int x1 = Math.Min(imagenOriginal.Width - 1, x + mitad);
            int y1 = Math.Min(imagenOriginal.Height - 1, y + mitad);
            int total = (x1 - x0 + 1) * (y1 - y0 + 1);

            double sumaR = 0, sumaG = 0, sumaB = 0;
            double sumaCuadR = 0, sumaCuadG = 0, sumaCuadB = 0;

            for (int i = x0; i <= x1; i++)
                for (int j = y0; j <= y1; j++)
                {
                    Color c = imagenOriginal.GetPixel(i, j);
                    sumaR += c.R; sumaG += c.G; sumaB += c.B;
                    sumaCuadR += c.R * c.R; sumaCuadG += c.G * c.G; sumaCuadB += c.B * c.B;
                }

            double promR = sumaR / total, promG = sumaG / total, promB = sumaB / total;
            double varR = Math.Max(0, (sumaCuadR / total) - (promR * promR));
            double varG = Math.Max(0, (sumaCuadG / total) - (promG * promG));
            double varB = Math.Max(0, (sumaCuadB / total) - (promB * promB));
            double stdR = Math.Sqrt(varR), stdG = Math.Sqrt(varG), stdB = Math.Sqrt(varB);

            int idx = texturaAprendiendo;
            texturas[idx, 0] = promR;
            texturas[idx, 1] = promG;
            texturas[idx, 2] = promB;
            texturas[idx, 3] = stdR;
            texturas[idx, 4] = stdG;
            texturas[idx, 5] = stdB;
            texturaAprendida[idx] = true;

            textBoxInfo.Text = $"{nombresTextura[idx]} aprendida correctamente.";
            labelResultado.Text = $"Aprendida: {nombresTextura[idx]}";
            texturaAprendiendo = -1;
        }

        // Clasifica el punto donde se hizo clic, comparando con todas las texturas aprendidas
        private void ClasificarPunto(int x, int y)
        {
            // Verificar que haya al menos una textura aprendida
            int aprendidas = 0;
            for (int i = 0; i < 4; i++) if (texturaAprendida[i]) aprendidas++;
            if (aprendidas == 0)
            {
                labelResultado.Text = "Resultado: Primero aprende una textura.";
                return;
            }

            // Extraer ventana alrededor del punto
            int mitad = tamBloque / 2;
            int x0 = Math.Max(0, x - mitad);
            int y0 = Math.Max(0, y - mitad);
            int x1 = Math.Min(imagenOriginal.Width - 1, x + mitad);
            int y1 = Math.Min(imagenOriginal.Height - 1, y + mitad);
            int total = (x1 - x0 + 1) * (y1 - y0 + 1);

            double sumaR = 0, sumaG = 0, sumaB = 0;
            double sumaCuadR = 0, sumaCuadG = 0, sumaCuadB = 0;

            for (int i = x0; i <= x1; i++)
                for (int j = y0; j <= y1; j++)
                {
                    Color c = imagenOriginal.GetPixel(i, j);
                    sumaR += c.R; sumaG += c.G; sumaB += c.B;
                    sumaCuadR += c.R * c.R; sumaCuadG += c.G * c.G; sumaCuadB += c.B * c.B;
                }

            double promR = sumaR / total, promG = sumaG / total, promB = sumaB / total;
            double varR = Math.Max(0, (sumaCuadR / total) - (promR * promR));
            double varG = Math.Max(0, (sumaCuadG / total) - (promG * promG));
            double varB = Math.Max(0, (sumaCuadB / total) - (promB * promB));
            double[] vectorPunto = { promR, promG, promB, Math.Sqrt(varR), Math.Sqrt(varG), Math.Sqrt(varB) };

            // Comparar con todas las texturas aprendidas
            int mejor = -1;
            double mejorDist = double.MaxValue;
            for (int t = 0; t < 4; t++)
            {
                if (texturaAprendida[t])
                {
                    double[] vtext = { texturas[t, 0], texturas[t, 1], texturas[t, 2], texturas[t, 3], texturas[t, 4], texturas[t, 5] };
                    double d = DistanciaEuclidiana(vectorPunto, vtext);
                    if (d < mejorDist)
                    {
                        mejorDist = d;
                        mejor = t;
                    }
                }
            }

            // Mostrar resultado en el label
            if (mejor != -1)
            {
                labelResultado.Text = $"Resultado: {nombresTextura[mejor]} (distancia: {mejorDist:F2})";
                textBoxInfo.Text = $"Clasificación: {nombresTextura[mejor]} en ({x},{y})";
            }
            else
            {
                labelResultado.Text = "Resultado: No se pudo clasificar";
            }
        }

        // Cálculo de distancia euclidiana entre dos vectores
        private double DistanciaEuclidiana(double[] v1, double[] v2)
        {
            double suma = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                double d = v1[i] - v2[i];
                suma += d * d;
            }
            return Math.Sqrt(suma);
        }
    }
} 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication4
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Bitmap myBitmap = null;
        
        /*介面相關*/
        private void button1_Click(object sender, EventArgs e)     //載入圖檔
        {
            this.openFileDialog1.Filter = "所有檔案|*.*|BMP File| *.bmp|TIFF|*.tif;*.tiff|JPEG File|*.jpg| GIF File|*.gif";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)   //由對話框選取圖檔
            {
                myBitmap = new Bitmap(openFileDialog1.FileName);
                pictureBox1.Image = myBitmap;
            }
            pictureBox2.Image = null;
        }
        private void button2_Click(object sender, EventArgs e)  //圖片壓縮
        {
            button1.Enabled = false;
            button2.Enabled = false;
            trackBardistortion.Enabled = false;
            int[,,] ImgData = GetImgData(myBitmap);
            int[,] classes = new int[trackBardistortion.Value*2, 5]; //R,G,B，初始分群點，與各點比較的長度
            //Kmeans(ImgData, classes);
            randomclass(ImgData, classes);
            Bitmap processedBitmap = CreateBitmap(ImgData);
            pictureBox2.Image = processedBitmap;
            savebutton.Enabled = true;
            adjustbutton.Enabled = true;
        }
        private void trackBardistortion_Scroll(object sender, EventArgs e)  //失真率調整
        {
            trackBardistortionValue.Text = String.Format("{0:D}", trackBardistortion.Value);
        }
        private void adjustbutton_Click(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = true;
            savebutton.Enabled = false;
            adjustbutton.Enabled = false;
            trackBardistortion.Enabled = true;
            pictureBox2.Image = null;
            int[,,] ImgData = GetImgData(myBitmap);
        }
        private void savebutton_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            string curDir;
            curDir = Directory.GetCurrentDirectory();
            saveFileDialog1.InitialDirectory = curDir;

            saveFileDialog1.Filter = "JPG File|*.jpg";
            saveFileDialog1.Title = "儲存壓縮影像檔";
            saveFileDialog1.FilterIndex = 3;

            myBitmap = new Bitmap(pictureBox2.Image);

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                saveJpeg(saveFileDialog1.FileName, myBitmap, 100);
            }
            lossy_ratio(saveFileDialog1.FileName);
            button1.Enabled = true;
            button2.Enabled = true;
            savebutton.Enabled = false;
            adjustbutton.Enabled = false;
            trackBardistortion.Enabled = true;
        }
        private void button3_Click(object sender, EventArgs e)  //文字壓縮
        {
            //List<String> table = new List<string>();
            Dictionary<string, string> table = new Dictionary<string, string>();
            String sentence = File.ReadAllText(@"input.txt"); // 讀取輸入檔案

            StreamWriter outputFile = new StreamWriter(@"output.txt"); // 開啟輸出檔案
            BinaryWriter outputBinFile = new BinaryWriter(File.Open(@"output.bin", FileMode.Create));
            String outputBuffer = ""; // 輸出緩衝區
            Console.WriteLine(sentence + "\n"); // 顯示輸入字串
            int count = 0;

            for (int startIndex = 0; startIndex < sentence.Length;)
            {
                // 切半(要重複的話，不可能比字串一半長還長)
                int length = (sentence.Length / 2 > sentence.Length - startIndex ? sentence.Length - startIndex : sentence.Length / 2);
                String toFind = ""; // 要比對是否有重複的字串

                while (true)
                {
                    toFind = sentence.Substring(startIndex, length); // 截取字串
                    if (length == 1) break; // 只剩一個字
                    if (sentence.LastIndexOf(toFind) != sentence.IndexOf(toFind)) break; // 找到重複
                    length--;
                }

                if (!table.ContainsKey(toFind))
                {
                    table.Add(toFind, Convert.ToString(table.Count, 2)); // 找不到的話加進字典(用2進制)
                }

                outputBuffer += table[toFind]; // 對應到的編碼寫入緩衝區

                while (outputBuffer.Length >= 8) // 如果大於8個字
                {
                    count++;
                    byte writeByte = Convert.ToByte(outputBuffer.Substring(0, 8), 2); // 取前八個字轉為一個byte
                    Console.Write(writeByte.ToString("X2") + (count % 16 == 0 ? "\n" : (count % 2 == 0 ? "  " : " "))); // 16進制
                    outputFile.Write(writeByte.ToString("X2") + (count % 16 == 0 ? "\n" : (count % 2 == 0 ? "  " : " "))); // 寫入檔案
                    outputBuffer = outputBuffer.Remove(0, 8); // 把輸出的字從緩衝區清除

                }

                startIndex += length; // 讓接下來處理的從已經處理完的後面開始
            }

            //以下問題尚未解決

            /*while (outputBuffer.Length > 0) // 最後剩下的二進位值(未滿8個)補零填滿至8個
            {
                int i = outputBuffer.Length % 8; // 算出需要補多少個零

                //Console.Write(i);

                count++;
                byte writeByte = Convert.ToByte(outputBuffer + new String( '0' , 8 - i ), 2);

                Console.WriteLine(writeByte.ToString("X2") + "(" + outputBuffer + ")");
                
                //outputFile.Write(writeByte);
                outputFile.Write(writeByte.ToString("X2"));
                outputBuffer = "";
            }*/
            outputFile.Close(); // 結束輸出檔案
            outputBinFile.Close();
            FileInfo a = new FileInfo(@"output.txt");
            label8.Text = String.Format("{0:D}", sentence.Length / a.Length);

            MessageBox.Show("文字壓縮完成!!");


            /*foreach (KeyValuePair<string, string> raw in table) // 列出表格
            {
                Console.WriteLine(raw.Key.Replace("\n", "\\n") + " > " + raw.Value); // 把換行換成替代符號
            }*/

            Console.Write("Press Enter to continue...");
            Console.ReadLine(); // 等待使用者確認


        }
        
        //壓縮率計算
        private void lossy_ratio(string output_name)
        {
            //壓縮前檔案
            FileInfo input = new FileInfo(openFileDialog1.FileName);
            FileInfo output = new FileInfo(output_name);
            float ratio = 0;
            ratio = input.Length / output.Length;
            label8.Text = ratio.ToString("");
        }

        /*圖片相關*/
        private int[,,] GetImgData(Bitmap myBitmap) //取得原圖RGB
        {
            int[,,] ImgData = new int[myBitmap.Width, myBitmap.Height, 4];  // 4表顏色(RGB)+分群
            BitmapData byteArray = myBitmap.LockBits(new Rectangle(0, 0, myBitmap.Width, myBitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int ByteOfSkip = byteArray.Stride - byteArray.Width * 3;
            unsafe  //專案－＞屬性－＞建置－＞容許Unsafe程式碼須選取。           

            {
                byte* imgPtr = (byte*)(byteArray.Scan0);
                for (int y = 0; y < byteArray.Height; y++)
                {
                    for (int x = 0; x < byteArray.Width; x++)
                    {
                        ImgData[x, y, 2] = (int)*(imgPtr);
                        ImgData[x, y, 1] = (int)*(imgPtr + 1);
                        ImgData[x, y, 0] = (int)*(imgPtr + 2);
                        imgPtr += 3;
                    }
                    imgPtr += ByteOfSkip;
                }
            }

            myBitmap.UnlockBits(byteArray);
            return ImgData;
        }   
        public static Bitmap CreateBitmap(int[,,] ImgData)  //顯示預覽壓縮畫面
        {
            int Width = ImgData.GetLength(0);
            int Height = ImgData.GetLength(1);
            Bitmap myBitmap = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            BitmapData byteArray = myBitmap.LockBits(new Rectangle(0, 0, Width, Height),
                                           ImageLockMode.WriteOnly,
                                           PixelFormat.Format24bppRgb);

            //Padding bytes的長度
            int ByteOfSkip = byteArray.Stride - myBitmap.Width * 3;

            unsafe
            {   // 指標取出影像資料
                byte* imgPtr = (byte*)byteArray.Scan0;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        *imgPtr = (byte)ImgData[x, y, 2];       //B

                        *(imgPtr + 1) = (byte)ImgData[x, y, 1];   //G 

                        *(imgPtr + 2) = (byte)ImgData[x, y, 0];   //R  
                        imgPtr += 3;
                    }
                    imgPtr += ByteOfSkip; // 跳過Padding bytes
                }
            }
            myBitmap.UnlockBits(byteArray);
            return myBitmap;
        }   
        


        /*K-means相關*/
        private void Kmeans(int[,,] ImgData, int[,] classes)
        {
            int x = 0, y = 0;
            int Width = ImgData.GetLength(0);
            int Height = ImgData.GetLength(1);

            
            //第一次計算亂數取初始分群點            
            if(loopCount==1)
                randomclass(ImgData, classes);

            //計算各點到分群點的距離 ，並且離分群點較近的群聚再一起
            for (y = 0; y < Height; y++)
            {
                for (x = 0; x < Width; x++)
                {
                    double min = 100000;
                    for (int j = 0; j < trackBardistortion.Value; j++)
                    {
                        double mindDistance = distance(ImgData[x, y, 0], ImgData[x, y, 1], ImgData[x, y, 2], classes[j, 0], classes[j, 1], classes[j, 2]);
                        if (mindDistance < min)
                        {
                            min = mindDistance;
                            ImgData[x, y, 3] = j; // 離分群點較近的群聚再一起。
                        }
                    }
                }
            }

            //計算新的分群點
            int[,] tempClasses = new int[trackBardistortion.Value, 4];
            for (int j = 0; j < trackBardistortion.Value; j++)  //群
            {
                int[] tempRGB = new int[3];
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (ImgData[x, y, 3] == j)
                        {
                            // 計算加總同一群的RGB值及各組數量
                            tempRGB[0] += ImgData[x, y, 0];  //R
                            tempRGB[1] += ImgData[x, y, 1];  //G
                            tempRGB[2] += ImgData[x, y, 2];  //B
                            tempClasses[j, 3]++;             //各組數量  
                        }
                    }
                }
                //避免空集合*****************有時間就改成，要是空集合就找最大群的那群將裏頭的值除2*****************
                if (tempClasses[j, 3] == 0)
                    tempClasses[j, 3] = 1;  
                //計算各組RGB平均
                tempClasses[j, 0] = tempRGB[0] / tempClasses[j, 3]; //R
                tempClasses[j, 1] = tempRGB[1] / tempClasses[j, 3]; //G 
                tempClasses[j, 2] = tempRGB[2] / tempClasses[j, 3]; //B
            }

            //檢查是否分群點為一致，不一致再重新遞迴Kmeans 函式
            int k = 0;
            for (k = 0; k < trackBardistortion.Value; k++)
            {
                if ((tempClasses[k, 0] != classes[k, 0]) || (tempClasses[k, 1] != classes[k, 1]) || (tempClasses[k, 2] != classes[k, 2]))
                {
                    recursiveFlag = 1;
                    break;
                }
            }
            //避免一直找不到相同數值造成無線循環。
            if (k >= trackBardistortion.Value)  
                recursiveFlag = 0;
            if (recursiveFlag == 1)
            {
                for (int j = 0; j < trackBardistortion.Value; j++)
                {
                    classes[j, 0] = tempClasses[j, 0];
                    classes[j, 1] = tempClasses[j, 1];
                    classes[j, 2] = tempClasses[j, 2];
                }
                // 遞迴呼叫Kmeans函式
                loopCount++;
                Kmeans(ImgData, classes);
                
            }
            if (recursiveFlag == 0)
                sameclasscolor(ImgData, classes);
        }       
        private void sameclasscolor(int[,,] ImgData, int[,] classes)   //將同一群的顏色變成一樣
        {
            int Width = ImgData.GetLength(0);
            int Height = ImgData.GetLength(1);
            float lossy = 0;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int j = 0; j < trackBardistortion.Value; j++)
                    {
                        if(ImgData[x, y, 3] == j)
                        {
                            //失真率計算
                            lossy += Math.Abs(ImgData[x, y, 0] - classes[j, 0]) + Math.Abs(ImgData[x, y, 1] - classes[j, 1]) + Math.Abs(ImgData[x, y, 2] - classes[j, 2]);
                            //變成group的顏色
                            ImgData[x, y, 0] = classes[j, 0];   //R
                            ImgData[x, y, 1] = classes[j, 1];   //G
                            ImgData[x, y, 2] = classes[j, 2];   //B
                            classes[j, 3]++;
                        }
                    }
                }
            }
            lossy = lossy / (Height * Width*3);
            lossy_label.Text = lossy.ToString("");
        }
        private static double distance(int r, int g, int b, int cr, int cg, int cb)  //距離公式，cr,cg,cb為分群點RGB
        {
            return Math.Sqrt(Math.Pow((r - cr), 2) + Math.Pow((b - cb), 2) + Math.Pow((g - cg), 2));
        }
        private void randomclass(int[,,] ImgData, int[,] classes)  //亂數取初始分群點
        {

            int x = 0, y = 0, i = 0, j = 0;
            int Width = ImgData.GetLength(0);
            int Height = ImgData.GetLength(1);
            int[] all = new int[Width * Height];
            //取兩倍亂數，避免取的值太接近
            for (i = 0; i < trackBardistortion.Value * 2; i++)
            {
                Random random = new Random((int)DateTime.Now.Millisecond);
                x = random.Next(0, Width);
                Thread.Sleep(20);
                y = random.Next(0, Height);
                //將該位置的數值存入class
                classes[i, 0] = ImgData[x, y, 0]; //R
                classes[i, 1] = ImgData[x, y, 1]; //G
                classes[i, 2] = ImgData[x, y, 2]; //B
            }

            //進行SSE(加總各點到群組點的距離)，使用差距越大的像素
            i = 0;
            for (y = 0; y < Height; y++)
            {
                for (x = 0; x < Width; x++)
                {

                    for (j = 0; j < trackBardistortion.Value * 2; j++)
                    {
                        sumdDistance = distance(ImgData[x, y, 0], ImgData[x, y, 1], ImgData[x, y, 2], classes[j, 0], classes[j, 1], classes[j, 2]);
                        ImgData[x, y, 3] += (int)sumdDistance;
                    }
                    all[i] = ImgData[x, y, 3];
                    sumdDistance = 0;
                    i++;
                }
            }
            //運用快速排序法找中間值，並利用中間值找出起始群
            int middle = 0;
            middle = quickSearch(ImgData, all, trackBardistortion.Value);
            for (i = 0; i < trackBardistortion.Value; i++)
            {
                Random random = new Random((int)DateTime.Now.Millisecond);
                x = random.Next(0, Width);
                Thread.Sleep(20);
                y = random.Next(0, Height);
                if (ImgData[x, y, 3] >= middle) //超過中位數表示他是我們的分群代表點
                {
                    classes[i, 0] = ImgData[x, y, 0]; //R
                    classes[i, 1] = ImgData[x, y, 1]; //G
                    classes[i, 2] = ImgData[x, y, 2]; //B
                    //另存分群點作為字典進行查詢
                    writetxt(classes[i, 0], classes[i, 1], classes[i, 2]);
                }
            }
            //將距離較近的顏色分為同組
            int z = 0;
            int[] group = new int[Height * Width]; //轉為一維陣列
            byte[] S = new byte[Height * Width];
            for (y = 0; y < Height; y++)
            {
                for (x = 0; x < Width; x++)
                {
                    double min = 100000;
                    for ( j = 0; j < trackBardistortion.Value; j++) //循環每一個分群點
                    {   //計算與各分群點的距離，選擇最小的。
                        double mindDistance = distance(ImgData[x, y, 0], ImgData[x, y, 1], ImgData[x, y, 2], classes[j, 0], classes[j, 1], classes[j, 2]);
                        if (mindDistance < min)
                        {
                            min = mindDistance;
                            ImgData[x, y, 3] = j; // 離分群點較近的群聚再一起。
                        }
                    }
                    group[z] = ImgData[x, y, 3];
                    z++;
                }
            }
            RLE(group);//進行無失真壓縮
            sameclasscolor(ImgData, classes);
        }
        private void RLE(int[] group)//無失真壓縮
        {
            int count = 1;
            for(int i = 0; i < group.Length-1; i++)
            {
                if (group[i] == group[i + 1])
                    count++;
                else
                {
                    writetxt(group[i], count);
                    count = 1;
                }
            }
        }
        private int quickSearch(int[,,] ImgData, int[] all, int K) //運用快速排序法找中間值
        {
            //K起始時中數位置，例如若起始時S = "36,99,17,24,4"，則K等於3。
            int i = 0, small_length = 0, same_length = 0, big_length = 0;
            int Width = ImgData.GetLength(0);
            int Height = ImgData.GetLength(1);
            int[] small = new int[Width*Height];
            int[] same = new int[Width * Height];
            int[] big = new int[Width * Height];
            //選取任一數值做為比較值middle
            int middle = 0;
            Random random = new Random((int)DateTime.Now.Millisecond);
            middle = random.Next(0, all.Length);
            middle = all[middle];
            //比middle小的值分在第一群，一樣的在第二群，大的在第三群
            for (i = 0; i < Height*Width; i++)
            {
                if (all[i] < middle)
                {
                    small[small_length] = all[i];
                    small_length++;
                }
                else if (all[i] == middle)
                {
                    same[same_length] = all[i];
                    same_length++;
                }
                else
                {
                    big[big_length] = all[i];
                    big_length++;
                }
            }
            if (small_length >= K)
                return quickSearch(ImgData, small, K);
            else
            {
                if (small_length + same_length >= K)
                    return middle;
                else
                    return quickSearch(ImgData, big, K - small_length - same_length);
            }
        }
        private static int recursiveFlag = 0;
        private static int loopCount = 1;
        private static double sumdDistance = 0;


        /*儲存相關(內建)*/
        private void saveJpeg(string path, Bitmap img, long quality)
        {
            // Encoder parameter for image quality
            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            // Jpeg image codec
            ImageCodecInfo jpegCodec = this.getEncoderInfo("image/jpeg");


            if (jpegCodec == null)
                return;
            
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;

            img.Save(path, jpegCodec, encoderParams);
        }
        private ImageCodecInfo getEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            // Find the correct image codec
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        }

        /*編碼相關*/
        //字典編碼(文字)
        private void createdictionary(String[] sentence)
        {
            Dictionary<string, string> table = new Dictionary<string, string>();
            //String sentence = System.IO.File.ReadAllText(@"C:\Users\妍臻\Desktop\123.txt");
            String combine = "";
            int index = 0;
            bool add = false;
            for (int i = 0; i < sentence.Length; i++)
            {
                while (!add)
                {
                    combine += sentence[i];
                    try
                    {
                        table.Add(combine, Convert.ToString(index));
                        add = true;
                    }
                    catch (ArgumentException)
                    {
                        i++;
                        add = false;
                    }
                }
                add = false;
                combine = "";
                index++;
            }
            //writetxt(table);
        }
        
        
        /*bin 存入檔案*/
        private void writetxt(int data, int count)
        {
            //第二個參數設定為true表示不覆蓋原本的內容，把新內容直接添加進去
            StreamWriter sw = new StreamWriter("data", true);
            sw.WriteLine(data + " " + count);
            sw.Close();

        }   //output
        private void writetxt(int R, int G, int B)
        {
            //第二個參數設定為true表示不覆蓋原本的內容，把新內容直接添加進去
            StreamWriter sw = new StreamWriter("dictionary.txt", true);
            sw.WriteLine(R);
            sw.WriteLine(G);
            sw.WriteLine(B);
            sw.Close();
        }   //字典
        private void encode_Click(object sender, EventArgs e)
        {
            BinaryWriter bw;
            myBitmap = new Bitmap(pictureBox2.Image);
            int[,,] ImgData = GetImgData(myBitmap);
            int Width = ImgData.GetLength(0);
            int Height = ImgData.GetLength(1);
            //編碼

            // 創建文件
            try
            {
                bw = new BinaryWriter(new FileStream("myimg",FileMode.Create));
            }
            catch (IOException a)
            {
                Console.WriteLine(a.Message + "\n Cannot create file.");
                return;
            }
            // 寫入文件
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    
                    try
                    {
                        bw.Write(ImgData[x, y, 0].ToString());
                        bw.Write(ImgData[x, y, 1].ToString());
                        bw.Write(ImgData[x, y, 2].ToString());
                    }
                    catch (IOException a)
                    {
                        Console.WriteLine(a.Message + "\n Cannot write to file.");
                        return;
                    }
                }
            }
            bw.Close();
            MessageBox.Show("編碼完成!!");      
        }
        private void decode_Click(object sender, EventArgs e)
        {
            BinaryReader br;
            myBitmap = new Bitmap(pictureBox2.Image);
            int[,,] ImgData = GetImgData(myBitmap);
            int Width = ImgData.GetLength(0);
            int Height = ImgData.GetLength(1);
         
            // 讀取文件
            try
            {
                br = new BinaryReader(new FileStream("myimg",
                FileMode.Open));
            }
            catch (IOException a)
            {
                Console.WriteLine(a.Message + "\n Cannot open file.");
                return;
            }
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {

                    try
                    {
                        ImgData[x, y, 0] = br.ReadInt32();
                        //Console.WriteLine("Integer data: {0}", ImgData[x, y, 0]);
                        ImgData[x, y, 1] = br.ReadInt32();
                        //Console.WriteLine("Double data: {0}", ImgData[x, y, 1]);
                        ImgData[x, y, 2] = br.ReadInt32();
                        //Console.WriteLine("Boolean data: {0}", ImgData[x, y, 2]);
                    }
                    catch (IOException a)
                    {
                        Console.WriteLine(a.Message + "\n Cannot read from file.");
                        return;
                    }
                }
            }
            Bitmap processedBitmap = CreateBitmap(ImgData);
            pictureBox2.Image = processedBitmap;
            br.Close();
        }

    }
}

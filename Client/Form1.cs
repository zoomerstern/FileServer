using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Client
{
    public partial class Form1 : Form
    {
        bool on = false;
        int port = 11000;
        IPAddress ipAddr;
        IPEndPoint ipEndPoint;
        Socket client;
        string message;
        string directory="File";
        int sent;
        int recv;
        byte[] data;
        string select;
        Regex dir = new Regex(@"^[(]папка[)]*");
        string open = null;
        public Form1()
        {
            InitializeComponent();
            //Создание папок:
            DirectoryInfo d=new DirectoryInfo("Download");//Для скачивания
            if (d.Exists == false)
                d.Create();
            d = new DirectoryInfo(@"Download\Temp");//Для просмотра
            if (d.Exists == false)
                d.Create();
        }
        private void Connect()//Функция для соедниения с севером
        {
            try
            {
                if (textBox1.Text.ToString() != "")//Если ввели свой IP
                    ipAddr = IPAddress.Parse(textBox1.Text);
                else// По умолчанию
                    ipAddr = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(address => address.AddressFamily == AddressFamily.InterNetwork);//(IPAddress)Dns..GetHostAddresses(host);//System.Net.Dns.GetHostByName(host).AddressList[0]; ;//IPAddress.Parse("127.0.0.1"); //ipHost.AddressList[0];//
                ipEndPoint = new IPEndPoint(ipAddr, port);
                client = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // Соединяем сокет с удаленной точкой
                client.Connect(ipEndPoint);
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.ToString());
            }
        }
        private void clientClose()//Закрытия соединения
        {
            client.Shutdown(SocketShutdown.Both);
            client.Close();
            on = false;
            listBox1.Items.Clear();
        }
        private void listInsert()//вывод файлов сервера
        {
           
            listBox1.Items.Clear();//Очищаем список
            message = directory + "?file";//Команда вывода файлов
            Connect();//Подключение к серверу
            data = Encoding.UTF8.GetBytes(message);//Преобразуем в байтовый поток
            sent = client.Send(data);//Отправка команды
            while (true)
            {
                //прием данных           
                data = new byte[4096];//Байтовый поток
                recv = client.Receive(data);//Размер данных
                message = Encoding.UTF8.GetString(data, 0, recv);//Преобразуем в символьную информацию
                if (recv == 0)//Если данные не пришли 
                    break;//Выходим
                string[] ff = message.Split('?');//Разделяем символьный поток
                for (int i = 0; i < ff.Length; i++)//и записываем его в панель клиента
                {
                    if (ff[i] == "")
                        continue;
                    listBox1.Items.Insert(0, ff[i]);
                }

            }
        }
        private void on_off(object sender, EventArgs e)//Подкл/Откл от сервера
        {
            if (on == true)//Если подключен
            {
                clientClose();//Отсоединяемся
                return;
            }
            on = true;
            listInsert();//В противном подлючаемся
        }


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)//Выбор из списка
        {
            if (listBox1.SelectedIndex == -1)//если нажали на пустое поле
            {
                select = null; return;
            }
            select = listBox1.SelectedItem.ToString();//если выбрали, запоминаем
            if (open != null)//и скачиваем файл в папку для просотра
            {
                FileInfo f = new FileInfo(@"" + open);
                if (f.Exists == true)
                    f.Delete();
                open = null;
            }
        }

        private void save(bool Temp)//Функция для скачивания файла
        {
            message = directory+'\\'+select + "?save";
            data = Encoding.UTF8.GetBytes(message);
            Connect();
            //Отправляем данные через сокет
            sent = client.Send(data);
            FileStream f;
            if(Temp==true)//скачивание для просмотра или нет
                f = new FileStream(@"Download\" + select, FileMode.Create, FileAccess.Write);
            else
                f = new FileStream(@"Download\Temp\" + select, FileMode.Create, FileAccess.Write);
            while (true)
            {
                data = new byte[4096];
                recv = client.Receive(data);
                if (recv == 0)
                    break;
                f.Write(data, 0, recv);
            }
            f.Close();
        }
        private void button2_Click(object sender, EventArgs e)//Кнопка "Скачать"
        {
            if(select==null)//Выбрал ли файл из списка
                return;
            if (dir.IsMatch(select) == true)// не выбранна ли папка
                return;
            save(true);//функция скачивания
            MessageBox.Show("Файл " + select + " скачен ");
            select = null;
            open = null;
        }

        private void button3_Click(object sender, EventArgs e)//Удалить файл
        {
            if (select == null)
                return;
            if (dir.IsMatch(select) == true)
                return;
            message = directory+'\\'+select + "?delete";
            data = Encoding.UTF8.GetBytes(message);
            Connect();//Подключение к серверу
            //Отправляем данные через сокет
            sent = client.Send(data);
            clientClose();//Закртыие сокета
            button1.PerformClick();//Переподключение к серверу
            MessageBox.Show("Файл " + select + " удален " );
            select = null;//сброс выбора элемента
        }

        private void button4_Click(object sender, EventArgs e)//Перенести файл на сервер
        {

            if (on == false)
                return;
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();//Диалоговое окно для выбора файла

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
        

            if (openFileDialog1.ShowDialog() == DialogResult.OK)//Открытие окна
            {
                
                try
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null)//Если выбрали файл
                    {
                        message = directory+'\\'+Path.GetFileName(openFileDialog1.FileName) + "?blink";
                        data = Encoding.UTF8.GetBytes(message);
                        Connect();
                        //Отправляем данные через сокет
                        sent = client.Send(data);                
                        Connect();                   
                        message = "";
                        data = new byte[4096];
                        recv = client.Receive(data);
                        message = Encoding.UTF8.GetString(data, 0, recv);

                        if (message == "Go")//сигнал для переноса
                        {
                            using (myStream)
                            {
                                client.SendFile(@""+openFileDialog1.FileName);
                            }
                        }
                        clientClose();//обрываем соединение
                        button1.PerformClick();//переподключаемся
                        MessageBox.Show("Файл " + Path.GetFileName(openFileDialog1.FileName) + " перенесен ");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)//функция выхода
        {
            if (on == true)//Если подключенны
                clientClose();//Отключаемся
            if (open != null)//Удаляем файл для просмотра
            {
                FileInfo a = new FileInfo(@"" + open);
                if (a.Exists == true)
                    a.Delete();
            }
            this.Close();
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)//Двойной клик
        {
            
            if (select == null)
                return;
            if (dir.IsMatch(select) == false)//если выбрали папку, то выводим файлы этой папки
            { button7.PerformClick(); return; }
            
            string[] msel = select.Split(')');
            select = null;
            directory += '\\'+ msel[1];            
            listInsert();
        }

        private void button6_Click(object sender, EventArgs e)//переход на уровень выше
        {
            if (on == false)//если не подключенны к серверу
                return;
            string []mas=directory.Split('\\');
            if(mas.Length<2)
                return;

            directory = mas[0];
            for (int i = 1; i < (mas.Length - 1); i++)//восстонавливае путь, но на уровень выше
            {
                directory += '\\' + mas[i];
            }
            listInsert();//выводим файлы
        }

        private void button7_Click(object sender, EventArgs e)//Открыть/просмотреть файл
        {
            if (select == null)//выбран ли элемент списка?
                return;
            if (dir.IsMatch(select) == true)//Не выбранна ли папка?
                return;
            if ( new FileInfo("Download"+'\\'+"Temp" + '\\' + select).Exists==false)//Если существует
            {
                open = "Download" + '\\' + "Temp" + '\\' + select;//То сохраняем в папке для просмотра
                save(false);
            }
            System.Diagnostics.Process.Start(@"Download" + '\\' + "Temp" + '\\' + select);//открываем скаченный файл
        }

      

        private void button8_Click(object sender, EventArgs e)//обновить список
        {
            //clientClose();
            if (on == false)
                return;
            listInsert();//вывод файлов
            MessageBox.Show("Обновил");
                
        }

    }
}

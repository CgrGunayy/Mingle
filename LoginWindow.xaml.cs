using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MingleWPF
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            DatabaseHandler.InitializeDatabase();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = UserPasswordBox.Password;

            LoginMessage loginMessage = DatabaseHandler.Login(username, password);

            if (loginMessage == LoginMessage.SuccessfulLogin)
            {
                ProjectSetupWindow projectSetup = new ProjectSetupWindow();
                projectSetup.Show();
                this.Close();
            }
            else
            {
                if (loginMessage == LoginMessage.NoUserWithUsername)
                    MessageBox.Show("No User With This Username", "No User");
                else if (loginMessage == LoginMessage.IncorrectPassword)
                    MessageBox.Show("Incorrect Password", "Incorrect Password");
                else
                    MessageBox.Show("A System Error Occured", "System Error");
            }
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SignUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SignUpWindow signUp = new SignUpWindow();
            signUp.Show();
            this.Close();
        }
    }
}

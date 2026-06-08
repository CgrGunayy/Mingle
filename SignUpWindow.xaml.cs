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
    public partial class SignUpWindow : Window
    {
        public SignUpWindow()
        {
            InitializeComponent();
        }

        private void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string email = EmailTextBox.Text;
            string password = UserPasswordBox.Password;
            string passwordRepeat = UserPasswordRepeatBox.Password;

            if (password != passwordRepeat)
            {
                MessageBox.Show("Passwords Must Be The Same", "Repeat Password");
                return;
            }

            SignUpMessage signUpMessage = DatabaseHandler.SignUp(username, email, password);

            if (signUpMessage == SignUpMessage.SuccessfulSignUp)
            {
                MessageBox.Show("Successfuly Signed Up", "Signed Up");
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
            else
            {
                if (signUpMessage == SignUpMessage.UserWithSameUsername)
                    MessageBox.Show("This Username Is Used By Someone", "Used Username");
                else if (signUpMessage == SignUpMessage.InvalidShortPassword)
                    MessageBox.Show("This Password Is Too Short", "Short Password");
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

        private void SignIn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}

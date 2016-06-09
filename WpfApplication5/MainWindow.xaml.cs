using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DevExpress.Xpf.Core.Internal;

namespace WpfApplication5 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            new RefAndReturnTests().StringStringVisibility();
            InitializeComponent();
            var sc = new SomeClass() {};

            var isc = sc.Wrap2<ISomeClass>()
                //.DefineMember(x => x.Name)
                //.Name("Name2")
                //.BindingFlags(BindingFlags.NonPublic | BindingFlags.Instance)
                ////.FieldAccessor()
                //.EndMember()
                .Create();

            //isc.Method1(isc.Name);
            //isc.Name = "baby";
            object baby = "baby";
            isc.Method1(ref baby);
            int name = 0;
            MessageBox.Show((string)baby);
            //isc.Method2(ref name);
        }
    }

    public class SomeClass {
        //string Name { get; set; }
        string Name2 { get; set; }
        public void Method1(ref string name) {
            MessageBox.Show($"Hello {name}!");
            name = "dude";
        }

        public void Method2(ref int name) {
            name = 42;
        }
    }

    public interface ISomeClass {
        //[ReflectionHelperAttributes.FieldAccessor]
        //string Name { get; set; }
        void Method1(ref object name);
        //void Method2(ref int name);
    }
}

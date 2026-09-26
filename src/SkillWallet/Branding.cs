using System;
using System.Reflection;
using System.Windows.Media.Imaging;
namespace SkillWallet {
    public static class Branding {
        public static BitmapSource Load(string name){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SkillWallet."+name)){if(stream==null)throw new InvalidOperationException("缺少品牌图标素材。");var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();return image;}}
    }
}

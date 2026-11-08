using System.IO;
using Diorama.EditorTools;
using NUnit.Framework;

namespace Diorama.Tests
{
    public class Step15aTests
    {
        [Test]
        public void VersionCode_StartsAt10_AndAlwaysGoesUp()
        {
            Assert.AreEqual(10, ReleaseBuild.NextVersionCode(1));
            Assert.AreEqual(10, ReleaseBuild.NextVersionCode(9));
            Assert.AreEqual(11, ReleaseBuild.NextVersionCode(10));
            Assert.AreEqual(43, ReleaseBuild.NextVersionCode(42));
        }

        [Test]
        public void ReleaseManifest_RemovesMicAndBluetooth_KeepsTheRest()
        {
            string path = Path.Combine(Path.GetTempPath(), "pocketisle_manifest_test.xml");
            File.WriteAllText(path,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" package=\"x\">\n" +
                "  <uses-permission android:name=\"android.permission.INTERNET\" />\n" +
                "  <uses-permission android:name=\"android.permission.RECORD_AUDIO\" />\n" +
                "  <uses-permission android:name=\"android.permission.MODIFY_AUDIO_SETTINGS\" />\n" +
                "  <uses-permission android:name=\"android.permission.BLUETOOTH\" />\n" +
                "  <uses-permission android:name=\"com.oculus.permission.HAND_TRACKING\" />\n" +
                "</manifest>\n");
            try
            {
                Assert.AreEqual(3, ReleaseBuild.StripPermissions(path));
                string xml = File.ReadAllText(path);
                StringAssert.Contains("android.permission.INTERNET", xml);
                StringAssert.Contains("com.oculus.permission.HAND_TRACKING", xml);
                StringAssert.Contains("xmlns:tools", xml);
                foreach (string p in ReleaseBuild.RemovedPermissions)
                    StringAssert.Contains($"android:name=\"{p}\" tools:node=\"remove\"", xml);
                Assert.AreEqual(0, ReleaseBuild.StripPermissions(path), "running twice changes nothing");
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}

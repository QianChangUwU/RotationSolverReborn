import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("updater", Path(__file__).parents[1] / "update_pluginmaster.py")
updater = importlib.util.module_from_spec(spec)
spec.loader.exec_module(updater)


class PluginMasterTests(unittest.TestCase):
    def setUp(self):
        self.manifest = dict(InternalName="RotationSolver", AssemblyVersion="7.5.6.16",
                             DalamudApiLevel=15, Name="自动战斗", IconUrl="https://example.com/icon.png")
        self.other = dict(InternalName="Questionable", AssemblyVersion="15.1", custom=True)

    def update(self, entries):
        return updater.update(entries, self.manifest, "7.5.6.16", "QianChangUwU/RotationSolverReborn", now=123)

    def test_append_preserves_other_plugins(self):
        result = self.update([self.other])
        self.assertEqual(result[0], self.other)
        self.assertEqual(result[1]["IconUrl"], self.manifest["IconUrl"])
        self.assertTrue(result[1]["DownloadLinkInstall"].endswith("/v7.5.6.16/latest.zip"))

    def test_matches_identity_and_preserves_testing(self):
        old = dict(InternalName="RotationSolver", AssemblyVersion="7.5.6.15",
                   TestingAssemblyVersion="8.0.0.0", DownloadLinkTesting="testing", custom=42)
        result = self.update([self.other, old])
        self.assertEqual(result[0], self.other)
        self.assertEqual(result[1]["TestingAssemblyVersion"], "8.0.0.0")
        self.assertEqual(result[1]["custom"], 42)
        self.assertEqual(old["AssemblyVersion"], "7.5.6.15")

    def test_retry_is_noop(self):
        result = self.update([self.other])
        self.assertEqual(self.update(result), result)

    def test_duplicate_rejected(self):
        with self.assertRaises(ValueError):
            self.update([self.manifest, self.manifest])

    def test_downgrade_rejected(self):
        with self.assertRaises(ValueError):
            self.update([dict(InternalName="RotationSolver", AssemblyVersion="8.0.0.0")])

    def test_manifest_mismatch_rejected(self):
        self.manifest["AssemblyVersion"] = "1.0.0.0"
        with self.assertRaises(ValueError):
            self.update([])


if __name__ == "__main__":
    unittest.main()

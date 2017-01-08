using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GitGooey.Tests
{
    [TestFixture(TestOf = typeof(DifferentialPatch))]
    class DifferentialPatchTests
    {
        [Test]
        public void DoesItWork()
        {
            const string samplePatch = @"From 95819c29abcefb408dd8f91457306725f0509e89 Mon Sep 17 00:00:00 2001
From: Alex Lau <al@saucelabs.com>
Date: Mon, 17 Oct 2016 14:12:47 -0700
Subject: [PATCH] Fix --suppress-adb-kill-server command line argument

---
 lib/parser.js | 2 +-
 1 file changed, 1 insertion(+), 1 deletion(-)

diff --git a/lib/parser.js b/lib/parser.js
index 82a643b..5ee9185 100644
--- a/lib/parser.js
+++ b/lib/parser.js
@@ -326,7 +326,7 @@ const args = [
   }],
 
   [['--suppress-adb-kill-server'], {
-    dest: 'suppressAdbKillServer',
+    dest: 'suppressKillServer',
     defaultValue: false,
     action: 'storeTrue',
     required: false,
";

            var asd = new DifferentialPatch(samplePatch);
        }
    }
}

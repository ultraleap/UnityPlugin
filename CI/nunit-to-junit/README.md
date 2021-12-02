### nunit3-xunit -- Paul Hicks

Converts NUnit3 results to JUnit-style results. It deliberately drops some information: the intention is to produce a results file suitable for publishing to Jenkins via the JUnit publisher.

The Jenkins NUnit publisher ("NUnit plugin" for Jenkins) requires NUnit2-style results and isn't keeping up with the snazziness of the JUnit plugin. Of particular interest to me, the JUnit plugin allows for claiming of individual test failures. XML files produced by transforming NUnit3 results with the attached XLST file are suitable for publishing via the JUnit plugin.

The transform is usually used via nunit-console's --result option:

    nunit3-console.exe YourTestAssembly.dll --result=junit-results.xml;transform=nunit3-junit.xslt

This transform is XSLT 1.0 compliant. It would be simpler if it used XSL 2.0; for example, the for-each loop in the test-suite template could be reduced to a simple string-join. XSLT 1.0 was chosen so that it can be used with Powershell, which currently (February 2016) supports only XSLT 1.0.

If you would like to run this using Powershell, here is a minimal Powershell script which you can run from Jenkins via the Powershell plugin. It uses the default NUnit output file name TestResults.xml and transforms it to a new file junit-results.xml, which you can then publish to Jenkins using the JUnit plugin.

    $xml = Resolve-Path TestResult.xml
    $output = Join-Path ($pwd) junit-results.xml
    $xslt = New-Object System.Xml.Xsl.XslCompiledTransform;
    $xslt.Load("nunit3-junit.xslt");
    $xslt.Transform($xml, $output);
    
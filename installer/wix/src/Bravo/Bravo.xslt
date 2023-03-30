<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" 
                xmlns="http://schemas.microsoft.com/wix/2006/wi"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
                xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt" 
                exclude-result-prefixes="msxsl xsl wix" >

  <xsl:output method="xml" indent="yes" omit-xml-declaration="yes" />
  <xsl:strip-space elements="*"/>
  
  <!-- By default, copy all elements and nodes into the output -->
  <xsl:template match="@*|*">
    <xsl:copy>
      <xsl:apply-templates select="@*" />
      <xsl:apply-templates select="*" />
    </xsl:copy>
  </xsl:template>
  
  <!-- Find all <Component> elements with <File> elements with Source="" attribute contains '$(var.PublishFolder)\Bravo.exe' and tag it with the "BravoExeToRemove" key -->
  <xsl:key name="BravoExeToRemove" match="wix:Component[wix:File/@Source='$(var.PublishFolder)\Bravo.exe']" use="@Id" />
  <!-- If the element has the "BravoExeToRemove" key then don't render anything (i.e. removing it from the output) -->
  <xsl:template match="*[self::wix:Component or self::wix:ComponentRef][key('BravoExeToRemove', @Id)]" />

  <!--
  >> HACK to force MSI installer to downgrade a library during upgrade
  >> https://stackoverflow.com/questions/52993587/file-not-copied-only-during-upgrade
  >> https://stackoverflow.com/questions/44765707/how-to-exclude-files-in-wix-toolset
  -->

  <!-- Tag files with the "*ToRemove" key -->
  <xsl:key name="IdentityClientToRemove" match="wix:Component[wix:File/@Source='$(var.PublishFolder)\Microsoft.Identity.Client.dll']" use="@Id" />
  <xsl:key name="SystemTextJsonToRemove" match="wix:Component[wix:File/@Source='$(var.PublishFolder)\System.Text.Json.dll']" use="@Id" />
  <xsl:key name="NewtonsoftJsonToRemove" match="wix:Component[wix:File/@Source='$(var.PublishFolder)\Newtonsoft.Json.dll']" use="@Id" />
  <xsl:key name="WebView2ToRemove" match="wix:Component[contains(wix:File/@Source, '\WebView2Loader.dll') or wix:File/@Source='$(var.PublishFolder)\Microsoft.Web.WebView2.Core.dll' or wix:File/@Source='$(var.PublishFolder)\Microsoft.Web.WebView2.WinForms.dll' or wix:File/@Source='$(var.PublishFolder)\Microsoft.Web.WebView2.Wpf.dll']" use="@Id" />
  <!-- Don't render anything tagged with "*ToRemove" key -->
  <xsl:template match="*[self::wix:Component or self::wix:ComponentRef][key('IdentityClientToRemove', @Id)]" />
  <xsl:template match="*[self::wix:Component or self::wix:ComponentRef][key('SystemTextJsonToRemove', @Id)]" />
  <xsl:template match="*[self::wix:Component or self::wix:ComponentRef][key('NewtonsoftJsonToRemove', @Id)]" />
  <xsl:template match="*[self::wix:Component or self::wix:ComponentRef][key('WebView2ToRemove', @Id)]" />
  <!-- Reinserts as new components all files previously tagged and removed from the output -->
  <xsl:template match="wix:DirectoryRef">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
      <Directory Id="dir1WebView2LoaderNative" Name="runtimes">
        <Directory Id="dir2WebView2LoaderNative" Name="win-$(sys.BUILDARCH)">
          <Directory Id="dir3WebView2LoaderNative" Name="native">
            <Component Id="cmpWebView2LoaderNative" Guid="FB0060F8-B5A2-494D-AFA1-E425AF1BC6B4">
              <File Id="fileWebView2LoaderNative" KeyPath="no" Source="$(var.PublishFolder)\runtimes\win-$(sys.BUILDARCH)\native\WebView2Loader.dll" />
            </Component>
          </Directory>
        </Directory>
      </Directory>
    </xsl:copy>
  </xsl:template>
  <xsl:template match="wix:ComponentGroup">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
      <Component Id="cmpRemovedFilesBundle" Directory="INSTALLFOLDER" Guid="C95A6D04-CE29-449D-B54F-D4EB6AA49D79">
        <File Id="fileIdentityClient" KeyPath="no" Source="$(var.PublishFolder)\Microsoft.Identity.Client.dll" />
        <File Id="fileSystemTextJson" KeyPath="no" Source="$(var.PublishFolder)\System.Text.Json.dll" />
        <File Id="fileNewtonsoftJson" KeyPath="no" Source="$(var.PublishFolder)\Newtonsoft.Json.dll" />
        <File Id="fileWebView2Loader" KeyPath="no" Source="$(var.PublishFolder)\WebView2Loader.dll" />
        <File Id="fileWebView2Core" KeyPath="no" Source="$(var.PublishFolder)\Microsoft.Web.WebView2.Core.dll" />
        <File Id="fileWebView2WinForms" KeyPath="no" Source="$(var.PublishFolder)\Microsoft.Web.WebView2.WinForms.dll" />
        <File Id="fileWebView2Wpf" KeyPath="no" Source="$(var.PublishFolder)\Microsoft.Web.WebView2.Wpf.dll" />
      </Component>
      <ComponentRef Id="cmpWebView2LoaderNative" />
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
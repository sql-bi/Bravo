<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" 
                xmlns="http://schemas.microsoft.com/wix/2006/wi"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
                xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt" 
                exclude-result-prefixes="msxsl xsl wix" >

  <xsl:output method="xml" indent="yes" omit-xml-declaration="yes" />
  <xsl:strip-space elements="*"/>
  
  <xsl:template match="@*|*">
    <xsl:copy>
      <xsl:apply-templates select="@*" />
      <xsl:apply-templates select="*" />
    </xsl:copy>
  </xsl:template>

  <xsl:key name="RemoveBravoExe" match="wix:Component[wix:File/@Source='$(var.PublishFolder)\Bravo.exe']" use="@Id" />
  <xsl:template match="wix:Component[key('RemoveBravoExe', @Id)]" />
  <xsl:template match="wix:ComponentRef[key('RemoveBravoExe', @Id)]" />

  <xsl:key name="RemoveSystemTextJson" match="wix:Component[wix:File/@Source='$(var.PublishFolder)\System.Text.Json.dll']" use="@Id" />
  <xsl:template match="wix:Component[key('RemoveSystemTextJson', @Id)]" />
  <xsl:template match="wix:ComponentRef[key('RemoveSystemTextJson', @Id)]" />
    
  <xsl:key name="RemoveNewtonsoftJson" match="wix:Component[wix:File/@Source='$(var.PublishFolder)\Newtonsoft.Json.dll']" use="@Id" />
  <xsl:template match="wix:Component[key('RemoveNewtonsoftJson', @Id)]" />
  <xsl:template match="wix:ComponentRef[key('RemoveNewtonsoftJson', @Id)]" />

  <xsl:key name="RemoveWebView2" match="wix:Component[contains(wix:File/@Source, '\WebView2Loader.dll') or wix:File/@Source='$(var.PublishFolder)\Microsoft.Web.WebView2.Core.dll' or wix:File/@Source='$(var.PublishFolder)\Microsoft.Web.WebView2.WinForms.dll' or wix:File/@Source='$(var.PublishFolder)\Microsoft.Web.WebView2.Wpf.dll']" use="@Id" />
  <xsl:template match="wix:Component[key('RemoveWebView2', @Id)]" />
  <xsl:template match="wix:ComponentRef[key('RemoveWebView2', @Id)]" />

  <!-- HACK to force WiX and msi installers to downgrade a library during a msi upgrade -->
  <!-- https://stackoverflow.com/questions/52993587/file-not-copied-only-during-upgrade -->
  <xsl:template match="wix:DirectoryRef">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
      <Directory Id="dir1WebView2Native" Name="runtimes">
        <Directory Id="dir2WebView2Native" Name="win-$(sys.BUILDARCH)">
          <Directory Id="dir3WebView2Native" Name="native">
            <Component Id="cmpRemovedWebView2Loader" Guid="FB0060F8-B5A2-494D-AFA1-E425AF1BC6B4">
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
        <File Id="fileSystemTextJson" KeyPath="no" Source="$(var.PublishFolder)\System.Text.Json.dll" />
        <File Id="fileNewtonsoftJson" KeyPath="no" Source="$(var.PublishFolder)\Newtonsoft.Json.dll" />
        <File Id="fileWebView2Loader" KeyPath="no" Source="$(var.PublishFolder)\WebView2Loader.dll" />
        <File Id="fileWebView2Core" KeyPath="no" Source="$(var.PublishFolder)\Microsoft.Web.WebView2.Core.dll" />
        <File Id="fileWebView2WinForms" KeyPath="no" Source="$(var.PublishFolder)\Microsoft.Web.WebView2.WinForms.dll" />
        <File Id="fileWebView2Wpf" KeyPath="no" Source="$(var.PublishFolder)\Microsoft.Web.WebView2.Wpf.dll" />
      </Component>
      <ComponentRef Id="cmpRemovedWebView2Loader" />
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
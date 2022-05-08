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

  <xsl:key name="FilesToRemove" match="wix:Component[contains(wix:File/@Source, 'Bravo.exe') or contains(wix:File/@Source, 'System.Text.Json.dll') or contains(wix:File/@Source, 'Newtonsoft.Json.dll')]" use="@Id" />
  <xsl:template match="wix:Component[key('FilesToRemove', @Id)]" />
  <xsl:template match="wix:ComponentRef[key('FilesToRemove', @Id)]" />

  <!-- HACK to force WiX and msi installers to downgrade a library during a msi upgrade -->
  <!-- https://stackoverflow.com/questions/52993587/file-not-copied-only-during-upgrade -->
  <xsl:template match="wix:ComponentGroup">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()" />
      <Component Id="cmpBundledFilesToRemove" Directory="INSTALLFOLDER" Guid="C95A6D04-CE29-449D-B54F-D4EB6AA49D79">
        <File Id="fileSystemTextJsonDll" KeyPath="no" Source="$(var.PublishFolder)\System.Text.Json.dll" />
        <File Id="fileNewtonsoftJsonDll" KeyPath="no" Source="$(var.PublishFolder)\Newtonsoft.Json.dll" />
      </Component>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
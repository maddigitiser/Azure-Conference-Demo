<?xml version="1.0" encoding="utf-8"?>
<serviceModel xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" name="AzureConference.Azure" generation="1" functional="0" release="0" Id="7033e630-e6f4-4b4d-8234-f5039baddfc9" dslVersion="1.2.0.0" xmlns="http://schemas.microsoft.com/dsltools/RDSM">
  <groups>
    <group name="AzureConference.AzureGroup" generation="1" functional="0" release="0">
      <componentports>
        <inPort name="AzureConference:Endpoint1" protocol="http">
          <inToChannel>
            <lBChannelMoniker name="/AzureConference.Azure/AzureConference.AzureGroup/LB:AzureConference:Endpoint1" />
          </inToChannel>
        </inPort>
      </componentports>
      <settings>
        <aCS name="AzureConference:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/AzureConference.Azure/AzureConference.AzureGroup/MapAzureConference:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="AzureConferenceInstances" defaultValue="[1,1,1]">
          <maps>
            <mapMoniker name="/AzureConference.Azure/AzureConference.AzureGroup/MapAzureConferenceInstances" />
          </maps>
        </aCS>
      </settings>
      <channels>
        <lBChannel name="LB:AzureConference:Endpoint1">
          <toPorts>
            <inPortMoniker name="/AzureConference.Azure/AzureConference.AzureGroup/AzureConference/Endpoint1" />
          </toPorts>
        </lBChannel>
      </channels>
      <maps>
        <map name="MapAzureConference:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/AzureConference.Azure/AzureConference.AzureGroup/AzureConference/Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </setting>
        </map>
        <map name="MapAzureConferenceInstances" kind="Identity">
          <setting>
            <sCSPolicyIDMoniker name="/AzureConference.Azure/AzureConference.AzureGroup/AzureConferenceInstances" />
          </setting>
        </map>
      </maps>
      <components>
        <groupHascomponents>
          <role name="AzureConference" generation="1" functional="0" release="0" software="C:\Users\Tom\Documents\Visual Studio 2010\Projects\AzureConference\AzureConference.Azure\csx\Release\roles\AzureConference" entryPoint="base\x64\WaHostBootstrapper.exe" parameters="base\x64\WaIISHost.exe " memIndex="1792" hostingEnvironment="frontendadmin" hostingEnvironmentVersion="2">
            <componentports>
              <inPort name="Endpoint1" protocol="http" portRanges="80" />
            </componentports>
            <settings>
              <aCS name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="" />
              <aCS name="__ModelData" defaultValue="&lt;m role=&quot;AzureConference&quot; xmlns=&quot;urn:azure:m:v1&quot;&gt;&lt;r name=&quot;AzureConference&quot;&gt;&lt;e name=&quot;Endpoint1&quot; /&gt;&lt;/r&gt;&lt;/m&gt;" />
            </settings>
            <resourcereferences>
              <resourceReference name="DiagnosticStore" defaultAmount="[4096,4096,4096]" defaultSticky="true" kind="Directory" />
              <resourceReference name="EventStore" defaultAmount="[1000,1000,1000]" defaultSticky="false" kind="LogStore" />
            </resourcereferences>
          </role>
          <sCSPolicy>
            <sCSPolicyIDMoniker name="/AzureConference.Azure/AzureConference.AzureGroup/AzureConferenceInstances" />
            <sCSPolicyFaultDomainMoniker name="/AzureConference.Azure/AzureConference.AzureGroup/AzureConferenceFaultDomains" />
          </sCSPolicy>
        </groupHascomponents>
      </components>
      <sCSPolicy>
        <sCSPolicyFaultDomain name="AzureConferenceFaultDomains" defaultPolicy="[2,2,2]" />
        <sCSPolicyID name="AzureConferenceInstances" defaultPolicy="[1,1,1]" />
      </sCSPolicy>
    </group>
  </groups>
  <implements>
    <implementation Id="39c2bf7d-1a1e-4b68-ae8f-0016fdfba136" ref="Microsoft.RedDog.Contract\ServiceContract\AzureConference.AzureContract@ServiceDefinition.build">
      <interfacereferences>
        <interfaceReference Id="e001f9e5-d436-47c5-8bff-c0e5e652a284" ref="Microsoft.RedDog.Contract\Interface\AzureConference:Endpoint1@ServiceDefinition.build">
          <inPort>
            <inPortMoniker name="/AzureConference.Azure/AzureConference.AzureGroup/AzureConference:Endpoint1" />
          </inPort>
        </interfaceReference>
      </interfacereferences>
    </implementation>
  </implements>
</serviceModel>
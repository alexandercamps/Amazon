﻿using Xunit;

namespace Amazon.Ec2.Tests
{
    public class DescribeInstancesResponseTests
    {
        [Fact]
        public void X()
        {
            var text =
@"<DescribeInstancesResponse xmlns=""http://ec2.amazonaws.com/doc/2016-11-15/"">
    <requestId>8f7724cf-496f-496e-8fe3-example</requestId>
    <reservationSet>
        <item>
            <reservationId>r-1234567890abcdef0</reservationId>
            <ownerId>123456789012</ownerId>
            <groupSet/>
            <instancesSet>
                <item>
                    <instanceId>i-1234567890abcdef0</instanceId>
                    <imageId>ami-bff32ccc</imageId>
                    <instanceState>
                        <code>16</code>
                        <name>running</name>
                    </instanceState>
                    <privateDnsName>ip-192-168-1-88.eu-west-1.compute.internal</privateDnsName>
                    <dnsName>ec2-54-194-252-215.eu-west-1.compute.amazonaws.com</dnsName>
                    <reason/>
                    <keyName>my_keypair</keyName>
                    <amiLaunchIndex>0</amiLaunchIndex>
                    <productCodes/>
                    <instanceType>t2.micro</instanceType>
                    <launchTime>2015-12-22T10:44:05.000Z</launchTime>
                    <placement>
                        <availabilityZone>eu-west-1c</availabilityZone>
                        <groupName/>
                        <tenancy>default</tenancy>
                    </placement>
                    <monitoring>
                        <state>disabled</state>
                    </monitoring>
                    <subnetId>subnet-56f5f633</subnetId>
                    <vpcId>vpc-11112222</vpcId>
                    <privateIpAddress>192.168.1.88</privateIpAddress>
                    <ipAddress>54.194.252.215</ipAddress>
                    <sourceDestCheck>true</sourceDestCheck>
                    <groupSet>
                        <item>
                            <groupId>sg-e4076980</groupId>
                            <groupName>SecurityGroup1</groupName>
                        </item>
                    </groupSet>
                    <architecture>x86_64</architecture>
                    <rootDeviceType>ebs</rootDeviceType>
                    <rootDeviceName>/dev/xvda</rootDeviceName>
                    <blockDeviceMapping>
                        <item>
                            <deviceName>/dev/xvda</deviceName>
                            <ebs>
                                <volumeId>vol-1234567890abcdef0</volumeId>
                                <status>attached</status>
                                <attachTime>2015-12-22T10:44:09.000Z</attachTime>
                                <deleteOnTermination>true</deleteOnTermination>
                            </ebs>
                        </item>
                    </blockDeviceMapping>
                    <virtualizationType>hvm</virtualizationType>
                    <clientToken>xMcwG14507example</clientToken>
                    <tagSet>
                        <item>
                            <key>Name</key>
                            <value>Server_1</value>
                        </item>
                    </tagSet>
                    <hypervisor>xen</hypervisor>
                    <networkInterfaceSet>
                        <item>
                            <networkInterfaceId>eni-551ba033</networkInterfaceId>
                            <subnetId>subnet-56f5f633</subnetId>
                            <vpcId>vpc-11112222</vpcId>
                            <description>Primary network interface</description>
                            <ownerId>123456789012</ownerId>
                            <status>in-use</status>
                            <macAddress>02:dd:2c:5e:01:69</macAddress>
                            <privateIpAddress>192.168.1.88</privateIpAddress>
                            <privateDnsName>ip-192-168-1-88.eu-west-1.compute.internal</privateDnsName>
                            <sourceDestCheck>true</sourceDestCheck>
                            <groupSet>
                                <item>
                                    <groupId>sg-e4076980</groupId>
                                    <groupName>SecurityGroup1</groupName>
                                </item>
                            </groupSet>
                            <attachment>
                                <attachmentId>eni-attach-39697adc</attachmentId>
                                <deviceIndex>0</deviceIndex>
                                <status>attached</status>
                                <attachTime>2015-12-22T10:44:05.000Z</attachTime>
                                <deleteOnTermination>true</deleteOnTermination>
                            </attachment>
                            <association>
                                <publicIp>54.194.252.215</publicIp>
                                <publicDnsName>ec2-54-194-252-215.eu-west-1.compute.amazonaws.com</publicDnsName>
                                <ipOwnerId>amazon</ipOwnerId>
                            </association>
                            <privateIpAddressesSet>
                                <item>
                                    <privateIpAddress>192.168.1.88</privateIpAddress>
                                    <privateDnsName>ip-192-168-1-88.eu-west-1.compute.internal</privateDnsName>
                                    <primary>true</primary>
                                    <association>
                                    <publicIp>54.194.252.215</publicIp>
                                    <publicDnsName>ec2-54-194-252-215.eu-west-1.compute.amazonaws.com</publicDnsName>
                                    <ipOwnerId>amazon</ipOwnerId>
                                    </association>
                                </item>
                            </privateIpAddressesSet>
                        </item>
                    </networkInterfaceSet>
                    <ebsOptimized>false</ebsOptimized>
                </item>
            </instancesSet>
        </item>
    </reservationSet>
</DescribeInstancesResponse>";

            var response = DescribeInstancesResponse.Parse(text);
            
            Assert.Equal(1, response.Instances.Count);

            var instance = response.Instances[0];

            Assert.Equal("eu-west-1c", instance.Placement.AvailabilityZone);
            Assert.Equal("i-1234567890abcdef0", instance.InstanceId);
            Assert.Equal("t2.micro", instance.InstanceType);
            Assert.Equal("xen", instance.Hypervisor);


            Assert.Equal(1, instance.NetworkInterfaces.Length);
            Assert.Equal("eni-551ba033", instance.NetworkInterfaces[0].NetworkInterfaceId);
            Assert.Equal("vpc-11112222", instance.NetworkInterfaces[0].VpcId);

        }
    }
}

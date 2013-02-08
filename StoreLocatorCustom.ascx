<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="StoreLocatorCustom.ascx.cs" Inherits="SitefinityWebApp.Custom.StoreLocatorCustom" %>

<style>
    html, body, #map_canvas
    {
        margin: 0;
        padding: 0;
        height: 100%;
    }

    .storeLocator
    {
         padding: 10px; 
         border:1px solid Black;
         margin: 10px 0px 10px 0px;
    }

    .storeLocator .list
    {
         margin-top:10px;
         width:100%;
    }

    .storeLocator .column1
    {
         width:300px;
    }

    .storeLocator .yourZip
    {
        padding-right:10px;
        font-weight: bold;
    }

    .storeLocator .findStores
    {
    }

    .storeLocator .storeCount
    {
        padding-top:5px;
    }

    .storeLocator .distance
    {
        padding-left:15px;
    }
</style>

<script type="text/javascript" src="https://maps.googleapis.com/maps/api/js?key=AIzaSyA2OGXl_YKdtBFvOSb7sxyGvf9ePBaipSA&v=3.exp&sensor=false"></script>
<script type="text/javascript">
    var defaultLat = <asp:Literal id="litDefaultLat" runat="server" />;
    var defaultLong = <asp:Literal id="litDefaultLong" runat="server" />;
    var map;
    function initialize() {
        showMap(defaultLat, defaultLong);
    }

    function showMap(lat, long)
    {
        var mapOptions = {
            zoom: 14,
            center: new google.maps.LatLng(lat, long),
            mapTypeId: google.maps.MapTypeId.ROADMAP
        };
        var map = new google.maps.Map(document.getElementById("map_canvas"), mapOptions);
        var marker = new google.maps.Marker({ position: mapOptions.center, title: "" });
        // To add the marker to the map, call setMap();
        marker.setMap(map);
    }

    google.maps.event.addDomListener(window, 'load', initialize);
</script> 

<div class="storeLocator">
    <span class="yourZip">Your Zip Code:&nbsp;<asp:TextBox ID="txtSourceZip" runat="server" Columns="6"></asp:TextBox></span>
    <span class="findStores"><asp:Button ID="btnFindStores" Text="Find Stores" runat="server" /></span>
    <span class="distance">Within:&nbsp;
        <asp:DropDownList id="ddlDistance" AutoPostBack="true" runat="server">
            <asp:ListItem Text="All Stores" Value="0" />
            <asp:ListItem Text="10 miles" Value="10" />
            <asp:ListItem Text="50 miles" Value="50" />
            <asp:ListItem Text="75 miles" Value="75" />
            <asp:ListItem Text="100 miles" Value="100" />
        </asp:DropDownList>
    </span>
    
    <div class="storeCount">
        <asp:Label ID="lblStoreCount" runat="server"></asp:Label>&nbsp;stores
    </div>
    <table style="width:100%" class="list" cellspacing="0" cellpadding="0">
    <tr>
        <td class="column1" valign="top">        
            <telerik:RadListView runat="server" ID="listStores">
            <ItemTemplate>
                <div style="padding: 10px 10px 10px 10px;">
                    <b><a href='javascript:showMap(<%# Eval("Latitude")%> ,<%# Eval("Longtitude")%>)'><%# Eval("Title")%></a></b>
                    <br />
                    <%# Eval("Address")%>
                    <br />
                    <%# Eval("City")%>, <%# Eval("State")%> <%# Eval("Zip")%>
                    <br />
                    <%# Eval("Phone")%>
                    <br />
                    <span style='display:<%# Eval("Distance").ToString() == "0.00" ? "none" : "block"%>'>Distance: <%# Eval("Distance")%> miles</span>
                </div>
            </ItemTemplate>
            </telerik:RadListView>
        </td>
        <td valign="top">
            <div id="map_canvas" style="width:100%; height:400px;"></div>
        </td>
    </tr>
    </table>
</div>

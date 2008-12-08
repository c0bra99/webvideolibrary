<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebVideoLibraryViewer._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Web Video Library</title>
</head>
<body bgcolor="#c0c0c0">
    <form id="form1" runat="server">
    <div style ="text-align:center">
        <asp:Label ID="Label1" runat="server" Text="Web Video Library" 
            Font-Size="XX-Large"></asp:Label>
        <br />
        <asp:Label ID="Label2" runat="server" 
            Text="By:  David Morrison &amp; Christian Cox"></asp:Label>
        <br />
    </div>
    <div>
        
        <asp:TreeView ID="treeView" runat="server" ExpandDepth="1" NodeIndent="100">
        </asp:TreeView>
    

    
    </div>
    </form>
</body>
</html>

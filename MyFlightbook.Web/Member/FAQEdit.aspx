﻿<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="FAQEdit.aspx.cs" Inherits="Member_FAQEdit" ValidateRequest="false" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<%@ Register src="../Controls/mfbHtmlEdit.ascx" tagname="mfbHtmlEdit" tagprefix="uc1" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    Edit FAQ
</asp:Content>
<asp:Content ID="Content3" runat="server" ContentPlaceHolderID="cpTopForm">
    <asp:Panel ID="Panel2" runat="server">
        <h2>New FAQ Item.</h2>
        <table>
            <tr>
                <td>Category:
                </td>
                <td><asp:TextBox ID="txtCategory" Width="600px" runat="Server"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>Question:
                </td>
                <td><asp:TextBox ID="txtQuestion" Width="600px" runat="Server"></asp:TextBox>
                </td>
            </tr>
            <tr>
                <td>Answer:
                </td>
                <td>
                    <uc1:mfbHtmlEdit ID="txtAnswer" Width="600px" runat="server" />
                </td>
            </tr>
        </table>
        <asp:Button ID="btnInsert" runat="Server" Text="Insert" CommandName="Insert" OnClick="btnInsert_Click" UseSubmitBehavior="False" />
    </asp:Panel>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
    <h2>Existing FAQ Items</h2>
    <asp:UpdatePanel ID="UpdatePanel2" runat="server">
        <ContentTemplate>
            <asp:GridView ID="gvFAQ" runat="server" AllowSorting="True" 
                AutoGenerateColumns="False" AutoGenerateDeleteButton="True" 
                AutoGenerateEditButton="True" DataKeyNames="idFAQ" DataSourceID="sqlFAQ" 
                EnableModelValidation="True" ShowFooter="False" 
                onrowupdating="gvFAQ_RowUpdating">
                <Columns>
                    <asp:BoundField DataField="idFAQ" HeaderText="idFAQ" InsertVisible="False" 
                        ReadOnly="True" SortExpression="idFAQ" />
                    <asp:BoundField DataField="Category" HeaderText="Category" 
                        SortExpression="Category" />
                    <asp:BoundField DataField="Question" HeaderText="Question" 
                        SortExpression="Question" />
                    <asp:TemplateField HeaderText="Answer">
                        <ItemTemplate><%# Eval("Answer") %></ItemTemplate>
                        <EditItemTemplate>
                            <uc1:mfbHtmlEdit ID="txtAnswer" runat="server" Width="100%" Text='<%# Eval("Answer") %>' />
                        </EditItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>
                    No FAQ items found
                </EmptyDataTemplate>
            </asp:GridView>
            
            <asp:SqlDataSource ID="sqlFAQ" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                DeleteCommand="DELETE FROM FAQ WHERE idFAQ=?idFAQ" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                SelectCommand="SELECT * FROM FAQ ORDER BY idFAQ DESC" 
                UpdateCommand="UPDATE FAQ SET Category=?Category, Question=?Question, Answer=?Answer WHERE idFAQ=?idFAQ;" >
                <DeleteParameters>
                    <asp:Parameter Direction="Input" Type="Int32" Name="idFAQ" />
                </DeleteParameters>
                <UpdateParameters>
                    <asp:Parameter Direction="Input" Type="Int32" Name="idFAQ" />
                    <asp:Parameter Direction="Input" Type="String" Name="Category" />
                    <asp:Parameter Direction="Input" Type="String" Name="Question" />
                    <asp:Parameter Direction="Input" Type="String" Name="Answer" />
                </UpdateParameters>
            </asp:SqlDataSource>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>


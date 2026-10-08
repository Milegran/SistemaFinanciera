<%@ Page Title="Home Page" Language="VB" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.vb" Inherits="SistemaFinanciera._Default" %>

<asp:Content ID="BodyContent"
    ContentPlaceHolderID="MainContent"
    runat="server">

    <div class="page-header">
        <h1>Dashboard</h1>
        <p>Resumen general de la actividad financiera.</p>
    </div>

    <div class="card-financiera">
        <h5>Bienvenido al Sistema de Gestión Financiera</h5>
        <p>
            Desde este panel podrá administrar clientes,
            solicitudes, préstamos y pagos.
        </p>
    </div>

</asp:Content>

<%@ Page Title="Clientes" Language="VB" AutoEventWireup="false"
    MasterPageFile="~/Site.Master"
    CodeBehind="Clientes.aspx.vb"
    Inherits="SistemaFinanciera.Clientes" %>

<asp:Content ID="Content1"
    ContentPlaceHolderID="HeadContent"
    runat="server">
</asp:Content>

<asp:Content ID="Content2"
    ContentPlaceHolderID="MainContent"
    runat="server">

<div class="page-header">
    <h1>Clientes</h1>
    <p>Administración de clientes registrados en el sistema.</p>
</div>


<!-- ==========================================
     FORMULARIO NUEVO / EDITAR CLIENTE
     ========================================== -->

<asp:Panel ID="pnlFormularioCliente"
    runat="server"
    Visible="False"
    CssClass="card-financiera mb-4">

    <div class="d-flex justify-content-between align-items-center mb-4">

        <div>
            <h5 class="mb-1">Registrar cliente</h5>
            <small class="text-muted">
                Complete la información del nuevo cliente.
            </small>
        </div>

        <span class="badge bg-light text-dark border">
            Nuevo cliente
        </span>

    </div>


    <div class="row g-3">

        <!-- Identificación -->

        <div class="col-md-4">

            <label class="form-label">
                Tipo de identificación
            </label>

            <asp:DropDownList ID="ddlTipoIdentificacion"
                runat="server"
                CssClass="form-select">

                <asp:ListItem Text="Seleccione..." Value="" />
                <asp:ListItem Text="Cédula" Value="CEDULA" />
                <asp:ListItem Text="Pasaporte" Value="PASAPORTE" />

            </asp:DropDownList>

        </div>


        <div class="col-md-4">

            <label class="form-label">
                Número de identificación
            </label>

            <asp:TextBox ID="txtIdentificacion"
                runat="server"
                CssClass="form-control"
                placeholder="Ej. 8-123-456">
            </asp:TextBox>

        </div>


        <!-- Fecha de nacimiento -->

        <div class="col-md-4">

            <label class="form-label">
                Fecha de nacimiento
            </label>

            <asp:TextBox ID="txtFechaNacimiento"
                runat="server"
                TextMode="Date"
                CssClass="form-control">
            </asp:TextBox>

        </div>


        <!-- Nombre -->

        <div class="col-md-6">

            <label class="form-label">
                Nombres
            </label>

            <asp:TextBox ID="txtNombres"
                runat="server"
                CssClass="form-control"
                placeholder="Nombres del cliente">
            </asp:TextBox>

        </div>


        <div class="col-md-6">

            <label class="form-label">
                Apellidos
            </label>

            <asp:TextBox ID="txtApellidos"
                runat="server"
                CssClass="form-control"
                placeholder="Apellidos del cliente">
            </asp:TextBox>

        </div>


        <!-- Contacto -->

        <div class="col-md-6">

            <label class="form-label">
                Teléfono
            </label>

            <asp:TextBox ID="txtTelefono"
                runat="server"
                CssClass="form-control"
                placeholder="Ej. 6000-0000">
            </asp:TextBox>

        </div>


        <div class="col-md-6">

            <label class="form-label">
                Correo electrónico
            </label>

            <asp:TextBox ID="txtCorreo"
                runat="server"
                TextMode="Email"
                CssClass="form-control"
                placeholder="cliente@correo.com">
            </asp:TextBox>

        </div>


        <!-- Dirección -->

        <div class="col-12">

            <label class="form-label">
                Dirección
            </label>

            <asp:TextBox ID="txtDireccion"
                runat="server"
                CssClass="form-control"
                placeholder="Dirección residencial">
            </asp:TextBox>

        </div>


        <!-- Información laboral -->

        <div class="col-md-6">

            <label class="form-label">
                Ocupación
            </label>

            <asp:TextBox ID="txtOcupacion"
                runat="server"
                CssClass="form-control"
                placeholder="Ocupación del cliente">
            </asp:TextBox>

        </div>


        <div class="col-md-6">

            <label class="form-label">
                Ingresos mensuales
            </label>

            <div class="input-group">

                <span class="input-group-text">$</span>

                <asp:TextBox ID="txtIngresos"
                    runat="server"
                    TextMode="Number"
                    CssClass="form-control"
                    placeholder="0.00">
                </asp:TextBox>

            </div>

        </div>

    </div>


    <!-- Botones -->

    <div class="d-flex justify-content-end gap-2 mt-4">

        <asp:Button ID="btnCancelar"
            runat="server"
            Text="Cancelar"
            CssClass="btn btn-light border"
            CausesValidation="False" />

        <asp:Button ID="btnGuardarCliente"
            runat="server"
            Text="Guardar cliente"
            CssClass="btn btn-primary" />

    </div>

</asp:Panel>


<!-- ==========================================
     LISTADO
     ========================================== -->

<asp:Panel ID="pnlListadoClientes"
    runat="server"
    CssClass="card-financiera">

    <div class="d-flex justify-content-between align-items-center mb-4">

        <div>
            <h5 class="mb-1">Listado de clientes</h5>

            <small class="text-muted">
                Consulte y administre la información de los clientes.
            </small>
        </div>

        <asp:Button ID="btnNuevoCliente"
            runat="server"
            Text="+ Nuevo cliente"
            CssClass="btn btn-primary" />

    </div>


    <!-- Búsqueda -->

    <div class="row g-3 mb-3">

        <div class="col-md-6">

            <asp:TextBox ID="txtBuscar"
                runat="server"
                CssClass="form-control"
                placeholder="Buscar por nombre, cédula o correo...">
            </asp:TextBox>

        </div>


        <div class="col-md-3">

            <asp:DropDownList ID="ddlEstado"
                runat="server"
                CssClass="form-select">

                <asp:ListItem Text="Todos los estados" Value="" />
                <asp:ListItem Text="Activos" Value="1" />
                <asp:ListItem Text="Inactivos" Value="0" />

            </asp:DropDownList>

        </div>


        <div class="col-md-3">

            <asp:Button ID="btnBuscar"
                runat="server"
                Text="Buscar"
                CssClass="btn btn-outline-primary w-100" />

        </div>

    </div>


    <!-- Tabla -->

    <div class="table-responsive">

        <asp:GridView ID="gvClientes"
            runat="server"
            AutoGenerateColumns="False"
            CssClass="table table-hover align-middle"
            GridLines="None"
            EmptyDataText="No hay clientes registrados.">

            <Columns>

                <asp:BoundField
                    DataField="Identificacion"
                    HeaderText="Identificación" />

                <asp:BoundField
                    DataField="NombreCompleto"
                    HeaderText="Cliente" />

                <asp:BoundField
                    DataField="Telefono"
                    HeaderText="Teléfono" />

                <asp:BoundField
                    DataField="Correo"
                    HeaderText="Correo" />

                <asp:BoundField
                    DataField="Estado"
                    HeaderText="Estado" />

            </Columns>

        </asp:GridView>

    </div>

</asp:Panel>
    </asp:Content>
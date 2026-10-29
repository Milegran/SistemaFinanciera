<%@ Page Title="Iniciar sesión" Language="VB" AutoEventWireup="false" CodeBehind="Acceso.aspx.vb" Inherits="SistemaFinanciera.Acceso" %>
<!DOCTYPE html>
<html lang="es">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <meta name="theme-color" content="#f0f4f8" />
    <title>Iniciar sesión | CapitalWest</title>
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/Financiera.css?v=capitalwest-mysql-2") %>" />
    <link rel="stylesheet" href="<%= ResolveUrl("~/Content/Acceso.css?v=20261024") %>" />
</head>
<body class="auth-page">
    <div class="auth-form" data-user-id="<%= usuario.ClientID %>" data-password-id="<%= clave.ClientID %>" data-submit-id="<%= entrar.ClientID %>">
    <form id="form1" runat="server">
        <main class="auth-layout" aria-labelledby="titulo-acceso">
            <section class="auth-card" aria-label="Formulario de inicio de sesión">
                <div class="auth-card-content">
                    <a class="auth-brand" href="<%= ResolveUrl("~/Acceso.aspx") %>" aria-label="CapitalWest">
                        <img src="<%= ResolveUrl("~/Content/Images/capitalwest-login.png") %>" width="56" height="31" alt="" fetchpriority="high" />
                        <span>Capital<strong>West</strong></span><small>PRO</small>
                    </a>

                    <header class="auth-heading">
                        <h1 id="titulo-acceso">Iniciar sesión</h1>
                        <p>Accede a tu cuenta institucional</p>
                    </header>

                    <asp:Panel ID="alta" runat="server" Visible="false" CssClass="auth-first-admin">
                        <p>Esta base todavía no tiene usuarios. Crea el primer administrador desde este equipo.</p>
                        <div class="auth-field">
                            <label for="<%= nombreCompleto.ClientID %>">Nombre completo</label>
                            <asp:TextBox ID="nombreCompleto" runat="server" MaxLength="160" CssClass="form-control auth-input" autocomplete="name" placeholder="Ej. Rafael Barrios" />
                        </div>
                        <div class="auth-field">
                            <label for="<%= correo.ClientID %>">Correo</label>
                            <asp:TextBox ID="correo" runat="server" MaxLength="254" TextMode="Email" CssClass="form-control auth-input" autocomplete="email" />
                        </div>
                    </asp:Panel>

                    <div class="auth-alert" role="alert" aria-live="assertive"><asp:Literal ID="mensaje" Mode="Encode" runat="server" /></div>

                    <div class="auth-field">
                        <label for="<%= usuario.ClientID %>">Usuario o correo</label>
                        <div class="auth-input-wrap">
                            <svg aria-hidden="true" class="auth-field-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                <path d="M15.75 6a3.75 3.75 0 1 1-7.5 0 3.75 3.75 0 0 1 7.5 0ZM4.5 20.1a7.5 7.5 0 0 1 15 0A17.9 17.9 0 0 1 12 21.75c-2.68 0-5.22-.58-7.5-1.65Z" stroke-linecap="round" stroke-linejoin="round" />
                            </svg>
                            <asp:TextBox ID="usuario" runat="server" MaxLength="254" CssClass="form-control auth-input auth-input-with-icon" autocomplete="username" autocapitalize="none" spellcheck="false" placeholder="ejemplo@correo.com" required="required" aria-describedby="usernameError" />
                        </div>
                        <span class="auth-field-error" id="usernameError" hidden>Ingresa tu usuario o correo.</span>
                    </div>

                    <div class="auth-field">
                        <label for="<%= clave.ClientID %>">Contraseña</label>
                        <div class="auth-input-wrap">
                            <svg aria-hidden="true" class="auth-field-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                                <path d="M16.5 10.5V6.75a4.5 4.5 0 0 0-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 0 0 2.25-2.25v-6.75a2.25 2.25 0 0 0-2.25-2.25H6.75a2.25 2.25 0 0 0-2.25 2.25v6.75a2.25 2.25 0 0 0 2.25 2.25Z" stroke-linecap="round" stroke-linejoin="round" />
                            </svg>
                            <asp:TextBox ID="clave" runat="server" TextMode="Password" MaxLength="128" CssClass="form-control auth-input auth-input-with-icon auth-input-with-action" autocomplete="current-password" placeholder="••••••••" required="required" aria-describedby="passwordError" />
                            <button type="button" class="auth-password-toggle" id="togglePassword" aria-controls="<%= clave.ClientID %>" aria-label="Mostrar contraseña" aria-pressed="false">Mostrar</button>
                        </div>
                        <span class="auth-field-error" id="passwordError" hidden>Ingresa tu contraseña.</span>
                    </div>

                    <asp:Panel ID="confirmacion" runat="server" Visible="false" CssClass="auth-field">
                        <label for="<%= claveConfirmada.ClientID %>">Confirmar contraseña</label>
                        <asp:TextBox ID="claveConfirmada" runat="server" TextMode="Password" MaxLength="20" CssClass="form-control auth-input" autocomplete="new-password" />
                    </asp:Panel>

                    <div class="auth-options">
                        <label class="auth-remember" for="rememberUser">
                            <input id="rememberUser" type="checkbox" />
                            <span>Recordar usuario</span>
                        </label>
                        <button class="auth-link" type="button" id="openRecovery">¿Olvidó su contraseña?</button>
                    </div>

                    <div class="auth-submit-wrap">
                        <asp:Button ID="entrar" runat="server" CausesValidation="false" Text="Iniciar sesión" CssClass="auth-submit" />
                        <span class="auth-loading" id="loginLoading" aria-hidden="true" hidden></span>
                    </div>

                    <p class="auth-privacy">Tu contraseña se verifica de forma segura y nunca se guarda en texto plano.</p>
                </div>

                <footer class="auth-footer">
                    <span>CapitalWest · Sistema de gestión financiera</span>
                    <span>Versión 1.0</span>
                </footer>
            </section>

            <aside class="auth-visual" aria-label="Presentación de CapitalWest">
                <div class="auth-glow auth-glow-top" aria-hidden="true"></div>
                <div class="auth-glow auth-glow-bottom" aria-hidden="true"></div>
                <div class="auth-visual-content">
                    <span class="auth-badge"><i aria-hidden="true"></i> Plataforma financiera corporativa</span>
                    <h2>Tu operación financiera,<br />en un solo lugar.</h2>
                    <p>Gestiona clientes, préstamos y pagos con una vista clara de cada etapa.</p>

                    <div class="auth-preview" aria-hidden="true">
                        <div class="auth-preview-main">
                            <div class="auth-preview-head">
                                <div><strong>Gestión financiera</strong><span>Vista ilustrativa</span></div>
                                <span class="auth-preview-menu">•••</span>
                            </div>
                            <div class="auth-chart">
                                <span></span><span></span><span></span><span></span><span></span><span></span><span></span><span></span>
                            </div>
                            <div class="auth-chart-labels"><span>Clientes</span><span>Préstamos</span><span>Pagos</span><span>Reportes</span></div>
                        </div>
                        <div class="auth-preview-side">
                            <span class="auth-preview-caption">CapitalWest</span>
                            <div class="auth-donut"><span></span></div>
                            <span class="auth-preview-side-label">Control de cartera</span>
                            <span class="auth-preview-side-note">Información organizada</span>
                        </div>
                    </div>
                </div>
                <div class="auth-visual-foot" aria-hidden="true"><span></span><span></span><span></span></div>
            </aside>
        </main>

        <div class="auth-dialog-backdrop" id="recoveryDialog" hidden>
            <section class="auth-dialog" role="dialog" aria-modal="true" aria-labelledby="recoveryTitle" aria-describedby="recoveryText">
                <button type="button" class="auth-dialog-close" id="closeRecovery" aria-label="Cerrar">×</button>
                <span class="auth-dialog-icon" aria-hidden="true">?</span>
                <h2 id="recoveryTitle">Recuperar acceso</h2>
                <p id="recoveryText">La recuperación automática por correo todavía no está configurada. Contacta al administrador del sistema para restablecer tu contraseña.</p>
                <button type="button" class="auth-dialog-button" id="confirmRecovery">Entendido</button>
            </section>
        </div>
        <script src="<%= ResolveUrl("~/Scripts/Acceso.js?v=20261024") %>" defer="defer"></script>
    </form>
    </div>
</body>
</html>

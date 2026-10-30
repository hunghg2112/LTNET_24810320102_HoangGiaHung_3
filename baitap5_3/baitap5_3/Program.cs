using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace CafePOSSystem
{
    public class frmMainPOS : Form
    {
        // Chuỗi kết nối CSDL SQL Server
        private string connectionString = @"Server=localhost\SQLEXPRESS;Database=CafePOSDB;Integrated Security=True;TrustServerCertificate=True;";

        // TAB 1: POS CONTROLS
        private FlowLayoutPanel flpTables;
        private ComboBox cboCategory, cboDish;
        private NumericUpDown numQuantity;
        private Button btnAddDish, btnPay;
        private DataGridView dgvBill;
        private Label lblSelectedTable, lblTotalAmount;
        private int currentSelectedTableId = -1;
        private int currentHoaDonId = -1;

        // TAB 2: MANAGEMENT CONTROLS
        private DataGridView dgvMenuMgmt;
        private TextBox txtDishName;
        private NumericUpDown numPrice;
        private ComboBox cboCategoryMgmt;
        private Button btnAddMenu, btnEditMenu, btnDeleteMenu;
        private int selectedMonId = -1;

        public frmMainPOS()
        {
            TaoGiaoDien();
            LoadTableLayout();
            LoadCategories();
            LoadMenuManagementData();
        }

        private void TaoGiaoDien()
        {
            this.Text = "Hệ Thống Quản Lý Quán Cafe & Bán Hàng (POS Cafe System)";
            this.Size = new Size(1100, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            TabControl tabControl = new TabControl { Dock = DockStyle.Fill };
            TabPage tabPOS = new TabPage("Bán hàng (POS)");
            TabPage tabMgmt = new TabPage("Quản lý Thực đơn");

            // ==========================================
            // TAB 1: GIAO DIỆN BÁN HÀNG & SƠ ĐỒ BÀN
            // ==========================================
            // 1. Sơ đồ bàn (Bên trái)
            GroupBox gbTables = new GroupBox { Text = "Sơ đồ bàn", Location = new Point(10, 10), Size = new Size(400, 620) };
            flpTables = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
            gbTables.Controls.Add(flpTables);
            tabPOS.Controls.Add(gbTables);

            // 2. Khu vực Order & Hóa đơn (Bên phải)
            GroupBox gbOrder = new GroupBox { Text = "Chi tiết hóa đơn", Location = new Point(420, 10), Size = new Size(650, 620) };

            lblSelectedTable = new Label { Text = "Chưa chọn bàn", Location = new Point(15, 25), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.Blue };
            gbOrder.Controls.Add(lblSelectedTable);

            // Chọn món
            gbOrder.Controls.Add(new Label { Text = "Danh mục:", Location = new Point(15, 65), AutoSize = true });
            cboCategory = new ComboBox { Location = new Point(90, 62), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboCategory.SelectedIndexChanged += CboCategory_SelectedIndexChanged;
            gbOrder.Controls.Add(cboCategory);

            gbOrder.Controls.Add(new Label { Text = "Món ăn:", Location = new Point(255, 65), AutoSize = true });
            cboDish = new ComboBox { Location = new Point(315, 62), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            gbOrder.Controls.Add(cboDish);

            gbOrder.Controls.Add(new Label { Text = "SL:", Location = new Point(485, 65), AutoSize = true });
            numQuantity = new NumericUpDown { Location = new Point(515, 62), Width = 50, Minimum = 1, Maximum = 100, Value = 1 };
            gbOrder.Controls.Add(numQuantity);

            btnAddDish = new Button { Text = "Thêm món", Location = new Point(572, 58), Width = 68, Height = 30, BackColor = Color.LightSkyBlue };
            btnAddDish.Click += BtnAddDish_Click;
            gbOrder.Controls.Add(btnAddDish);

            // DataGridView hiển thị hóa đơn tạm
            dgvBill = new DataGridView
            {
                Location = new Point(15, 100),
                Size = new Size(620, 440),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White
            };
            gbOrder.Controls.Add(dgvBill);

            // Tổng tiền & Thanh toán
            lblTotalAmount = new Label { Text = "Tổng tiền: 0 VNĐ", Location = new Point(15, 560), AutoSize = true, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.DarkRed };
            gbOrder.Controls.Add(lblTotalAmount);

            btnPay = new Button { Text = "THANH TOÁN", Location = new Point(475, 555), Width = 160, Height = 45, BackColor = Color.LightGreen, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
            btnPay.Click += BtnPay_Click;
            gbOrder.Controls.Add(btnPay);

            tabPOS.Controls.Add(gbOrder);

            // ==========================================
            // TAB 2: QUẢN LÝ THỰC ĐƠN (CRUD 3-TIER / DISCONNECTED)
            // ==========================================
            GroupBox gbInputs = new GroupBox { Text = "Thông tin món ăn", Location = new Point(10, 10), Size = new Size(350, 620) };

            gbInputs.Controls.Add(new Label { Text = "Tên món:", Location = new Point(15, 40), AutoSize = true });
            txtDishName = new TextBox { Location = new Point(110, 37), Width = 210 };
            gbInputs.Controls.Add(txtDishName);

            gbInputs.Controls.Add(new Label { Text = "Danh mục:", Location = new Point(15, 80), AutoSize = true });
            cboCategoryMgmt = new ComboBox { Location = new Point(110, 77), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
            gbInputs.Controls.Add(cboCategoryMgmt);

            gbInputs.Controls.Add(new Label { Text = "Đơn giá (VNĐ):", Location = new Point(15, 120), AutoSize = true });
            numPrice = new NumericUpDown { Location = new Point(110, 117), Width = 210, Maximum = 10000000, Increment = 5000 };
            gbInputs.Controls.Add(numPrice);

            btnAddMenu = new Button { Text = "Thêm mới", Location = new Point(15, 180), Width = 95, Height = 35, BackColor = Color.LightGreen };
            btnAddMenu.Click += BtnAddMenu_Click;
            gbInputs.Controls.Add(btnAddMenu);

            btnEditMenu = new Button { Text = "Cập nhật", Location = new Point(120, 180), Width = 95, Height = 35, BackColor = Color.Khaki };
            btnEditMenu.Click += BtnEditMenu_Click;
            gbInputs.Controls.Add(btnEditMenu);

            btnDeleteMenu = new Button { Text = "Xóa món", Location = new Point(225, 180), Width = 95, Height = 35, BackColor = Color.LightCoral };
            btnDeleteMenu.Click += BtnDeleteMenu_Click;
            gbInputs.Controls.Add(btnDeleteMenu);

            tabMgmt.Controls.Add(gbInputs);

            dgvMenuMgmt = new DataGridView
            {
                Location = new Point(370, 18),
                Size = new Size(700, 612),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White
            };
            dgvMenuMgmt.SelectionChanged += DgvMenuMgmt_SelectionChanged;
            tabMgmt.Controls.Add(dgvMenuMgmt);

            tabControl.TabPages.Add(tabPOS);
            tabControl.TabPages.Add(tabMgmt);
            this.Controls.Add(tabControl);
        }

        // ==========================================================
        // YÊU CẦU 1: NẠP VÀ HIỂN THỊ SƠ ĐỒ BÀN ĐỘNG (VISUAL TABLE LAYOUT)
        // ==========================================================
        private void LoadTableLayout()
        {
            flpTables.Controls.Clear();
            string query = "SELECT MaBan, TenBan, TrangThai FROM BanAn";

            using (SqlDataAdapter da = new SqlDataAdapter(query, connectionString))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    int tableId = Convert.ToInt32(row["MaBan"]);
                    string tableName = row["TenBan"].ToString();
                    string status = row["TrangThai"].ToString();

                    Button btnTable = new Button
                    {
                        Size = new Size(110, 90),
                        Text = $"{tableName}\n({status})",
                        Tag = tableId,
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        Margin = new Padding(8)
                    };

                    // Bàn Trống -> Màu xanh, Bàn Có khách -> Màu đỏ
                    if (status == "Trống")
                    {
                        btnTable.BackColor = Color.LightGreen;
                    }
                    else
                    {
                        btnTable.BackColor = Color.LightCoral;
                    }

                    btnTable.Click += BtnTable_Click;
                    flpTables.Controls.Add(btnTable);
                }
            }
        }

        // Sự kiện khi chọn một bàn trên sơ đồ
        private void BtnTable_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            currentSelectedTableId = (int)btn.Tag;
            lblSelectedTable.Text = $"Đang chọn: {btn.Text.Split('\n')[0]}";

            LoadBillForTable(currentSelectedTableId);
        }

        // Nạp danh sách món đã đặt của bàn (Disconnected Model: SqlDataAdapter + DataTable)
        private void LoadBillForTable(int tableId)
        {
            currentHoaDonId = GetUnpaidHoaDonId(tableId);

            if (currentHoaDonId != -1)
            {
                string query = @"SELECT m.TenMon AS [Tên món], cthd.SoLuong AS [Số lượng], cthd.DonGia AS [Đơn giá], 
                                (cthd.SoLuong * cthd.DonGia) AS [Thành tiền]
                                FROM ChiTietHoaDon cthd
                                JOIN MonAn m ON cthd.MaMon = m.MaMon
                                WHERE cthd.MaHoaDon = @MaHoaDon";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaHoaDon", currentHoaDonId);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            dgvBill.DataSource = dt;

                            // Định dạng hiển thị tiền
                            if (dgvBill.Columns["Đơn giá"] != null) dgvBill.Columns["Đơn giá"].DefaultCellStyle.Format = "N0";
                            if (dgvBill.Columns["Thành tiền"] != null) dgvBill.Columns["Thành tiền"].DefaultCellStyle.Format = "N0";

                            // Tính tổng tiền
                            decimal total = 0;
                            foreach (DataRow r in dt.Rows)
                            {
                                total += Convert.ToDecimal(r["Thành tiền"]);
                            }
                            lblTotalAmount.Text = $"Tổng tiền: {total:N0} VNĐ";
                        }
                    }
                }
            }
            else
            {
                dgvBill.DataSource = null;
                lblTotalAmount.Text = "Tổng tiền: 0 VNĐ";
            }
        }

        private int GetUnpaidHoaDonId(int tableId)
        {
            string query = "SELECT MaHoaDon FROM HoaDon WHERE MaBan = @MaBan AND TrangThai = 0";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaBan", tableId);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : -1;
                }
            }
        }

        // ==========================================================
        // YÊU CẦU 2: CHỨC NĂNG GỌI MÓN (ORDER)
        // ==========================================================
        private void LoadCategories()
        {
            string query = "SELECT MaDanhMuc, TenDanhMuc FROM DanhMuc";
            using (SqlDataAdapter da = new SqlDataAdapter(query, connectionString))
            {
                DataTable dt1 = new DataTable();
                DataTable dt2 = new DataTable();
                da.Fill(dt1);
                da.Fill(dt2);

                cboCategory.DataSource = dt1;
                cboCategory.DisplayMember = "TenDanhMuc";
                cboCategory.ValueMember = "MaDanhMuc";

                cboCategoryMgmt.DataSource = dt2;
                cboCategoryMgmt.DisplayMember = "TenDanhMuc";
                cboCategoryMgmt.ValueMember = "MaDanhMuc";
            }
        }

        private void CboCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboCategory.SelectedValue != null && int.TryParse(cboCategory.SelectedValue.ToString(), out int catId))
            {
                string query = "SELECT MaMon, TenMon FROM MonAn WHERE MaDanhMuc = @MaDanhMuc";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaDanhMuc", catId);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            cboDish.DataSource = dt;
                            cboDish.DisplayMember = "TenMon";
                            cboDish.ValueMember = "MaMon";
                        }
                    }
                }
            }
        }

        private void BtnAddDish_Click(object sender, EventArgs e)
        {
            if (currentSelectedTableId == -1)
            {
                MessageBox.Show("Vui lòng chọn bàn cần gọi món!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboDish.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn món ăn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedMonId = Convert.ToInt32(cboDish.SelectedValue);
            int quantity = (int)numQuantity.Value;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // 1. Nếu bàn chưa có hóa đơn tạm -> Tạo mới Hóa đơn & Cập nhật Bàn = 'Có khách'
                if (currentHoaDonId == -1)
                {
                    string insertHD = "INSERT INTO HoaDon (MaBan, GioVao, TrangThai) VALUES (@MaBan, GETDATE(), 0); SELECT SCOPE_IDENTITY();";
                    using (SqlCommand cmdHD = new SqlCommand(insertHD, conn))
                    {
                        cmdHD.Parameters.AddWithValue("@MaBan", currentSelectedTableId);
                        currentHoaDonId = Convert.ToInt32(cmdHD.ExecuteScalar());
                    }

                    string updateBan = "UPDATE BanAn SET TrangThai = N'Có khách' WHERE MaBan = @MaBan";
                    using (SqlCommand cmdBan = new SqlCommand(updateBan, conn))
                    {
                        cmdBan.Parameters.AddWithValue("@MaBan", currentSelectedTableId);
                        cmdBan.ExecuteNonQuery();
                    }
                }

                // 2. Lấy đơn giá món hiện tại
                decimal price = 0;
                string getPrice = "SELECT DonGia FROM MonAn WHERE MaMon = @MaMon";
                using (SqlCommand cmdP = new SqlCommand(getPrice, conn))
                {
                    cmdP.Parameters.AddWithValue("@MaMon", selectedMonId);
                    price = Convert.ToDecimal(cmdP.ExecuteScalar());
                }

                // 3. Thêm hoặc cộng dồn món vào ChiTietHoaDon
                string checkCT = "SELECT SoLuong FROM ChiTietHoaDon WHERE MaHoaDon = @MaHoaDon AND MaMon = @MaMon";
                using (SqlCommand cmdCheck = new SqlCommand(checkCT, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@MaHoaDon", currentHoaDonId);
                    cmdCheck.Parameters.AddWithValue("@MaMon", selectedMonId);
                    object existQty = cmdCheck.ExecuteScalar();

                    if (existQty != null)
                    {
                        string updateCT = "UPDATE ChiTietHoaDon SET SoLuong = SoLuong + @SoLuong WHERE MaHoaDon = @MaHoaDon AND MaMon = @MaMon";
                        using (SqlCommand cmdU = new SqlCommand(updateCT, conn))
                        {
                            cmdU.Parameters.AddWithValue("@SoLuong", quantity);
                            cmdU.Parameters.AddWithValue("@MaHoaDon", currentHoaDonId);
                            cmdU.Parameters.AddWithValue("@MaMon", selectedMonId);
                            cmdU.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string insertCT = "INSERT INTO ChiTietHoaDon (MaHoaDon, MaMon, SoLuong, DonGia) VALUES (@MaHoaDon, @MaMon, @SoLuong, @DonGia)";
                        using (SqlCommand cmdI = new SqlCommand(insertCT, conn))
                        {
                            cmdI.Parameters.AddWithValue("@MaHoaDon", currentHoaDonId);
                            cmdI.Parameters.AddWithValue("@MaMon", selectedMonId);
                            cmdI.Parameters.AddWithValue("@SoLuong", quantity);
                            cmdI.Parameters.AddWithValue("@DonGia", price);
                            cmdI.ExecuteNonQuery();
                        }
                    }
                }
            }

            // Cập nhật lại giao diện
            LoadTableLayout();
            LoadBillForTable(currentSelectedTableId);
        }

        // ==========================================================
        // YÊU CẦU 3: CHỨC NĂNG THANH TOÁN DÙNG SQLTRANSACTION
        // ==========================================================
        private void BtnPay_Click(object sender, EventArgs e)
        {
            if (currentSelectedTableId == -1 || currentHoaDonId == -1 || dgvBill.Rows.Count == 0)
            {
                MessageBox.Show("Bàn hiện tại không có hóa đơn để thanh toán!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Tính tổng tiền hóa đơn
            decimal totalAmount = 0;
            foreach (DataGridViewRow row in dgvBill.Rows)
            {
                totalAmount += Convert.ToDecimal(row.Cells["Thành tiền"].Value);
            }

            if (MessageBox.Show($"Xác nhận thanh toán cho Bàn {currentSelectedTableId}?\nTổng tiền: {totalAmount:N0} VNĐ",
                                "Thanh toán", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // Khởi tạo SqlTransaction đảm bảo tính toàn vẹn dữ liệu
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // 1. Cập nhật HoaDon: GioRa, TongTien, TrangThai = 1
                        string updateHD = @"UPDATE HoaDon 
                                           SET GioRa = GETDATE(), TongTien = @TongTien, TrangThai = 1 
                                           WHERE MaHoaDon = @MaHoaDon";
                        using (SqlCommand cmdHD = new SqlCommand(updateHD, conn, transaction))
                        {
                            cmdHD.Parameters.AddWithValue("@TongTien", totalAmount);
                            cmdHD.Parameters.AddWithValue("@MaHoaDon", currentHoaDonId);
                            cmdHD.ExecuteNonQuery();
                        }

                        // 2. Chuyển trạng thái BanAn về 'Trống'
                        string updateBan = "UPDATE BanAn SET TrangThai = N'Trống' WHERE MaBan = @MaBan";
                        using (SqlCommand cmdBan = new SqlCommand(updateBan, conn, transaction))
                        {
                            cmdBan.Parameters.AddWithValue("@MaBan", currentSelectedTableId);
                            cmdBan.ExecuteNonQuery();
                        }

                        // Hoàn tất giao dịch
                        transaction.Commit();

                        MessageBox.Show("Thanh toán thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Reset giao diện
                        LoadTableLayout();
                        LoadBillForTable(currentSelectedTableId);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Hủy giao dịch nếu có lỗi
                        MessageBox.Show("Lỗi trong quá trình thanh toán: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ==========================================================
        // YÊU CẦU 4: QUẢN LÝ THỰC ĐƠN (3-TIER / DISCONNECTED CRUD)
        // ==========================================================
        private void LoadMenuManagementData()
        {
            string query = @"SELECT m.MaMon AS [Mã món], m.TenMon AS [Tên món], d.TenDanhMuc AS [Danh mục], 
                            m.DonGia AS [Đơn giá], m.MaDanhMuc
                            FROM MonAn m
                            JOIN DanhMuc d ON m.MaDanhMuc = d.MaDanhMuc";

            using (SqlDataAdapter da = new SqlDataAdapter(query, connectionString))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgvMenuMgmt.DataSource = dt;

                if (dgvMenuMgmt.Columns["MaDanhMuc"] != null) dgvMenuMgmt.Columns["MaDanhMuc"].Visible = false;
                if (dgvMenuMgmt.Columns["Đơn giá"] != null) dgvMenuMgmt.Columns["Đơn giá"].DefaultCellStyle.Format = "N0";
            }
        }

        private void DgvMenuMgmt_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvMenuMgmt.SelectedRows.Count > 0)
            {
                DataGridViewRow row = dgvMenuMgmt.SelectedRows[0];
                selectedMonId = Convert.ToInt32(row.Cells["Mã món"].Value);
                txtDishName.Text = row.Cells["Tên món"].Value.ToString();
                numPrice.Value = Convert.ToDecimal(row.Cells["Đơn giá"].Value);
                cboCategoryMgmt.SelectedValue = row.Cells["MaDanhMuc"].Value;
            }
        }

        private void BtnAddMenu_Click(object sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtDishName.Text))
            {
                MessageBox.Show("Tên món ăn không được để trống!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (numPrice.Value <= 0)
            {
                MessageBox.Show("Đơn giá phải lớn hơn 0!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "INSERT INTO MonAn (TenMon, MaDanhMuc, DonGia) VALUES (@TenMon, @MaDanhMuc, @DonGia)";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenMon", txtDishName.Text.Trim());
                    cmd.Parameters.AddWithValue("@MaDanhMuc", cboCategoryMgmt.SelectedValue);
                    cmd.Parameters.AddWithValue("@DonGia", numPrice.Value);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Thêm món thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadMenuManagementData();
        }

        private void BtnEditMenu_Click(object sender, EventArgs e)
        {
            if (selectedMonId == -1) return;

            if (string.IsNullOrWhiteSpace(txtDishName.Text) || numPrice.Value <= 0)
            {
                MessageBox.Show("Vui lòng kiểm tra lại thông tin nhập hợp lệ!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string query = "UPDATE MonAn SET TenMon = @TenMon, MaDanhMuc = @MaDanhMuc, DonGia = @DonGia WHERE MaMon = @MaMon";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenMon", txtDishName.Text.Trim());
                    cmd.Parameters.AddWithValue("@MaDanhMuc", cboCategoryMgmt.SelectedValue);
                    cmd.Parameters.AddWithValue("@DonGia", numPrice.Value);
                    cmd.Parameters.AddWithValue("@MaMon", selectedMonId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadMenuManagementData();
        }

        private void BtnDeleteMenu_Click(object sender, EventArgs e)
        {
            if (selectedMonId == -1) return;

            if (MessageBox.Show("Bạn có chắc muốn xóa món ăn này khỏi thực đơn?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    string query = "DELETE FROM MonAn WHERE MaMon = @MaMon";
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@MaMon", selectedMonId);
                            conn.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Xóa món thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadMenuManagementData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể xóa món này do đã có dữ liệu trong hóa đơn cũ!\nChi tiết: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMainPOS());
        }
    }
}
// Helper function to show modal
function showModal(modalId) {
    if (typeof bootstrap === 'undefined' || !bootstrap.Modal) {
        console.error("Bootstrap Modal is not available.");
        alert("Lỗi: Không thể hiển thị modal.");
        return;
    }
    const modalElement = document.getElementById(modalId);
    if (modalElement) {
        new bootstrap.Modal(modalElement).show();
    } else {
        console.error(`Modal with ID ${modalId} not found.`);
    }
}

// Show Contract Detail Modal
function showContractDetail(id) {
    console.log("Opening contract detail for ID:", id);
    fetch(`?handler=ContractDetail&id=${id}`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        }
    })
        .then(res => {
            if (!res.ok) {
                return res.text().then(text => {
                    throw new Error("Lỗi server: " + (text || res.statusText));
                });
            }
            return res.json();
        })
        .then(data => {
            if (data.success) {
                const contract = data.contract;
                document.getElementById('detail-contractNumber').textContent = contract.contractNumber || '';
                document.getElementById('detail-contractName').textContent = contract.contractName || '';
                document.getElementById('detail-contractType').textContent = contract.contractType || '';
                document.getElementById('detail-contractStatus').textContent = contract.contractStatus || '';
                document.getElementById('detail-startDate').textContent = formatDate(contract.startDate) || '';
                document.getElementById('detail-endDate').textContent = formatDate(contract.endDate) || '';
                document.getElementById('detail-contractValue').textContent = formatNumber(contract.contractValue) + ' VNĐ' || '';
                document.getElementById('detail-contractYear').textContent = contract.contractYear || '';
                document.getElementById('detail-contractDate').textContent = formatDate(contract.contractDate) || '';
                document.getElementById('detail-contractTime').textContent = contract.contractTime || '';
                document.getElementById('detail-partner').textContent = contract.partnerName || '';
                document.getElementById('detail-company').textContent = contract.companyName || '';
                document.getElementById('detail-branch').textContent = contract.branchName || '';
                document.getElementById('detail-department').textContent = contract.departmentName || '';
                document.getElementById('detail-createBy').textContent = contract.createBy || '';
                document.getElementById('detail-createDate').textContent = formatDate(contract.createDate) || '';
                document.getElementById('detail-lastUpdateBy').textContent = contract.lastUpdateBy || '';
                document.getElementById('detail-lastUpdateDate').textContent = formatDate(contract.lastUpdateDate) || '';
                document.getElementById('detail-totalInvoiceValue').textContent = formatNumber(contract.totalInvoiceValue) + ' VNĐ' || '0 VNĐ';

                // Calculate and display payment status
                const contractValue = contract.contractValue || 0;
                const totalInvoiceValue = contract.totalInvoiceValue || 0;
                const paymentStatusElement = document.getElementById('detail-paymentStatus');
                if (totalInvoiceValue >= contractValue && contractValue > 0) {
                    paymentStatusElement.textContent = 'Đã thanh toán xong';
                    paymentStatusElement.className = 'text-success';
                } else if (totalInvoiceValue < contractValue) {
                    const remaining = contractValue - totalInvoiceValue;
                    paymentStatusElement.textContent = `Còn thiếu: ${formatNumber(remaining)} VNĐ`;
                    paymentStatusElement.className = 'text-danger';
                } else {
                    paymentStatusElement.textContent = 'Chưa thanh toán';
                    paymentStatusElement.className = 'text-warning';
                }

                // Populate contract files
                const contractFilesContainer = document.getElementById('detail-contract-files');
                contractFilesContainer.innerHTML = '';
                if (contract.contractFiles && contract.contractFiles.length > 0) {
                    contract.contractFiles.forEach(file => {
                        const fileElement = document.createElement('div');
                        fileElement.className = 'mb-1';
                        fileElement.innerHTML = `
                        <a href="?handler=DownloadFile&id=${file.autoID}" class="text-primary" download>
                            <i class="bi bi-file-earmark-arrow-down"></i> ${file.fileName}
                        </a>
                    `;
                        contractFilesContainer.appendChild(fileElement);
                    });
                } else {
                    contractFilesContainer.innerHTML = '<p>Không có tệp đính kèm</p>';
                }

                // Populate invoice files
                const invoiceFilesContainer = document.getElementById('detail-invoice-files');
                invoiceFilesContainer.innerHTML = '';
                if (contract.invoiceFiles && contract.invoiceFiles.length > 0) {
                    contract.invoiceFiles.forEach(file => {
                        const fileElement = document.createElement('div');
                        fileElement.className = 'mb-1';
                        fileElement.innerHTML = `
                        <a href="?handler=DownloadFile&id=${file.autoID}" class="text-primary" download>
                            <i class="bi bi-file-earmark-arrow-down"></i> ${file.fileName}
                        </a>
                    `;
                        invoiceFilesContainer.appendChild(fileElement);
                    });
                } else {
                    invoiceFilesContainer.innerHTML = '<p>Không có tệp đính kèm hóa đơn</p>';
                }

                showModal('contractDetailModal');
            } else {
                alert("Không thể lấy thông tin hợp đồng: " + (data.message || "Không xác định"));
            }
        })
        .catch(err => {
            console.error("Error fetching contract details:", err);
            alert("Lỗi: " + err.message);
        });
}

// Helper function to format date
function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN');
}

// Helper function to format number
function formatNumber(value) {
    if (value === null || value === undefined) return '';
    return new Intl.NumberFormat('vi-VN').format(value);
}
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

// Load partners
function loadPartners() {
    console.log("Loading partners...");
    fetch('?handler=Partners', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(res => res.json())
        .then(data => {
            const partnerSelect = document.getElementById('partnerID');
            partnerSelect.innerHTML = '<option value="">Chọn đối tác</option>';
            data.forEach(partner => {
                const option = document.createElement('option');
                option.value = partner.id;
                option.textContent = partner.companyName;
                partnerSelect.appendChild(option);
            });
        })
        .catch(err => {
            console.error('Lỗi khi tải danh sách đối tác:', err);
            alert("Lỗi khi tải danh sách đối tác.");
        });
}

// Load partners for edit modal and pre-select current partner
function loadPartnersForEdit(currentPartnerId) {
    fetch('?handler=Partners', {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(res => res.json())
        .then(data => {
            const partnerSelect = document.getElementById('editPartnerID');
            partnerSelect.innerHTML = '<option value="">Chọn đối tác</option>';
            data.forEach(partner => {
                const option = document.createElement('option');
                option.value = partner.id;
                option.textContent = partner.companyName;
                if (partner.id === currentPartnerId) {
                    option.selected = true;
                }
                partnerSelect.appendChild(option);
            });
        })
        .catch(err => {
            console.error('Lỗi khi tải danh sách đối tác:', err);
            alert("Lỗi khi tải danh sách đối tác.");
        });
}

// Show Contract Detail
function showContractDetail(id) {
    console.log("Opening contract detail for ID:", id);
    fetch(`?handler=ContractDetail&id=${id}`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
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
                document.getElementById('detail-department').textContent = contract.departmentName || '';
                document.getElementById('detail-createBy').textContent = contract.createBy || '';
                document.getElementById('detail-createDate').textContent = formatDate(contract.createDate) || '';
                document.getElementById('detail-lastUpdateBy').textContent = contract.lastUpdateBy || '';
                document.getElementById('detail-lastUpdateDate').textContent = formatDate(contract.lastUpdateDate) || '';
                document.getElementById('detail-totalInvoiceValue').textContent = formatNumber(contract.totalInvoiceValue) + ' VNĐ' || '0 VNĐ';

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
            console.error("Error:", err);
            alert("Lỗi: " + err.message);
        });
}

// Show Edit Contract Modal
function showEditContractModal(id) {
    console.log("Opening edit contract modal for ID:", id);
    fetch(`?handler=ContractDetail&id=${id}`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
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
                document.getElementById('editContractId').value = contract.autoID;
                document.getElementById('editContractName').value = contract.contractName || '';
                document.getElementById('editContractType').value = contract.contractType || '';
                document.getElementById('editContractNumber').value = contract.contractNumber || '';
                document.getElementById('editContractYear').value = contract.contractYear || '';
                document.getElementById('editContractValue').value = contract.contractValue || '';
                document.getElementById('editContractDate').value = formatDateForInput(contract.contractDate) || '';
                document.getElementById('editContractTime').value = contract.contractTime || '';
                document.getElementById('editStartDate').value = formatDateForInput(contract.startDate) || '';
                document.getElementById('editEndDate').value = formatDateForInput(contract.endDate) || '';

                // Load partners and pre-select the current partner
                loadPartnersForEdit(contract.partnerID);

                // Populate contract files
                const contractFilesContainer = document.getElementById('edit-contract-files');
                contractFilesContainer.innerHTML = '';
                if (contract.contractFiles && contract.contractFiles.length > 0) {
                    contract.contractFiles.forEach(file => {
                        const fileElement = document.createElement('div');
                        fileElement.className = 'mb-1';
                        fileElement.innerHTML = `
                        <span>${file.fileName}</span>
                        <button type="button" class="btn btn-danger btn-sm ms-2" onclick="removeFile(${file.autoID})">
                            <i class="bi bi-trash-fill"></i>
                        </button>
                    `;
                        contractFilesContainer.appendChild(fileElement);
                    });
                } else {
                    contractFilesContainer.innerHTML = '<p>Không có tệp đính kèm</p>';
                }

                // Populate invoice files
                const invoiceFilesContainer = document.getElementById('edit-invoice-files');
                invoiceFilesContainer.innerHTML = '';
                if (contract.invoiceFiles && contract.invoiceFiles.length > 0) {
                    contract.invoiceFiles.forEach(file => {
                        const fileElement = document.createElement('div');
                        fileElement.className = 'mb-1';
                        fileElement.innerHTML = `
                        <span>${file.fileName}</span>
                        <button type="button" class="btn btn-danger btn-sm ms-2" onclick="removeFile(${file.autoID})">
                            <i class="bi bi-trash-fill"></i>
                        </button>
                    `;
                        invoiceFilesContainer.appendChild(fileElement);
                    });
                } else {
                    invoiceFilesContainer.innerHTML = '<p>Không có hóa đơn</p>';
                }

                showModal('editContractModal');
            } else {
                alert("Không thể lấy thông tin hợp đồng: " + (data.message || "Không xác định"));
            }
        })
        .catch(err => {
            console.error("Error:", err);
            alert("Lỗi: " + err.message);
        });
}

// Show Add File/Invoice Modal
function showAddFileInvoiceModal(contractId) {
    console.log("Opening add file/invoice modal for contract ID:", contractId);
    document.getElementById('addFileInvoiceForm').reset();
    document.getElementById('addFileContractId').value = contractId;
    document.getElementById('addInvoiceCheckbox2').checked = false;
    toggleInvoiceFields2();
    showModal('addFileInvoiceModal');
}

// Format date
function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN');
}

// Format date for input
function formatDateForInput(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
}

// Format number
function formatNumber(value) {
    if (value === null || value === undefined) return '';
    return new Intl.NumberFormat('vi-VN').format(value);
}

// Show Add Contract Modal
function showAddContractModal() {
    console.log("Opening add contract modal");
    document.getElementById('addContractForm').reset();
    document.getElementById('addInvoiceCheckbox').checked = false;
    toggleInvoiceFields();
    loadPartners();
    showModal('addContractModal');
}

// Toggle Invoice Fields
function toggleInvoiceFields() {
    const invoiceFields = document.getElementById('invoiceFields');
    const checkbox = document.getElementById('addInvoiceCheckbox');
    invoiceFields.style.display = checkbox.checked ? 'block' : 'none';
}

// Toggle Invoice Fields for Add File/Invoice Modal
function toggleInvoiceFields2() {
    const invoiceFields = document.getElementById('invoiceFields2');
    const checkbox = document.getElementById('addInvoiceCheckbox2');
    invoiceFields.style.display = checkbox.checked ? 'block' : 'none';
}

// Save new contract
function saveNewContract() {
    const form = document.getElementById('addContractForm');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const formData = new FormData(form);
    fetch("?handler=AddContract", {
        method: 'POST',
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: formData
    })
        .then(res => {
            if (!res.ok) {
                return res.text().then(text => {
                    throw new Error("Lỗi server: " + text);
                });
            }
            return res.json();
        })
        .then(data => {
            if (data.success) {
                alert(`Thêm mới hợp đồng thành công`);
                location.reload();
            } else {
                alert("Thêm hợp đồng thất bại: " + (data.message || "Không xác định"));
            }
        })
        .catch(err => {
            console.error("Error saving contract:", err);
            alert("Lỗi khi lưu hợp đồng: " + err.message);
        });
}

// Save edited contract
function saveEditedContract() {
    const form = document.getElementById('editContractForm');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const formData = new FormData(form);
    fetch('?handler=EditContract', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: formData
    })
        .then(res => {
            if (!res.ok) {
                return res.text().then(text => {
                    throw new Error("Lỗi server: " + text);
                });
            }
            return res.json();
        })
        .then(data => {
            if (data.success) {
                alert('Cập nhật hợp đồng thành công');
                location.reload();
            } else {
                alert('Cập nhật hợp đồng thất bại: ' + (data.message || 'Không xác định'));
            }
        })
        .catch(err => {
            console.error('Lỗi khi cập nhật hợp đồng:', err);
            alert('Lỗi khi cập nhật hợp đồng: ' + err.message);
        });
}

// Save new file/invoice
function saveFileInvoice() {
    const form = document.getElementById('addFileInvoiceForm');
    const formData = new FormData(form);
    fetch('?handler=AddFileInvoice', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: formData
    })
        .then(res => {
            if (!res.ok) {
                return res.text().then(text => {
                    throw new Error("Lỗi server: " + text);
                });
            }
            return res.json();
        })
        .then(data => {
            if (data.success) {
                alert('Thêm tệp/hoá đơn thành công');
                location.reload();
            } else {
                alert('Thêm tệp/hoá đơn thất bại: ' + (data.message || 'Không xác định'));
            }
        })
        .catch(err => {
            console.error('Lỗi khi thêm tệp/hoá đơn:', err);
            alert('Lỗi khi thêm tệp/hoá đơn: ' + err.message);
        });
}

// Remove file
function removeFile(fileId) {
    if (confirm('Bạn có chắc chắn muốn xóa tệp này không?')) {
        fetch(`?handler=RemoveFile`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ fileId: fileId })
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    alert('Xóa tệp thành công');
                    // Reload the edit modal with updated data
                    const contractId = document.getElementById('editContractId').value;
                    showEditContractModal(contractId);
                } else {
                    alert('Xóa tệp thất bại: ' + (data.message || 'Không xác định'));
                }
            })
            .catch(err => {
                console.error('Lỗi khi xóa tệp:', err);
                alert('Lỗi khi xóa tệp: ' + err.message);
            });
    }
}

// Confirm delete contract
function confirmDeleteContract(id) {
    console.log("Confirm delete for contract ID:", id);
    document.getElementById('deleteContractId').value = id;
    showModal('deleteContractModal');
}

// Delete contract
function deleteContract() {
    const id = document.getElementById('deleteContractId').value;
    fetch("?handler=DeleteContract", {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        },
        body: JSON.stringify({ id: parseInt(id) })
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
                alert("Xóa hợp đồng thành công");
                location.reload();
            } else {
                alert("Xóa hợp đồng thất bại: " + (data.message || "Không xác định"));
            }
        })
        .catch(err => {
            console.error("Error deleting contract:", err);
            alert("Lỗi: " + err.message);
        });
}
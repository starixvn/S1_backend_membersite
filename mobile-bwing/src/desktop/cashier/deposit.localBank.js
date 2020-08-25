import React, { Component, createRef } from "react";
import Dropdown from "./deposit.dropdown";
import { number_format } from "../../common/common";

export class BankInfoModal extends Component {
	constructor(props) {
		super(props);
		this.state = {
			Show: false,
			UploadDisabled: true,
			SubmitDisabled: false,
			date: moment().format("YYYY-MM-DD HH:mm:ss"),
			RefNo: ""
		};
		this.filebox = createRef();
		this.dateBox = createRef();
	}
	componentDidMount() {
		$(this.dateBox.current).daterangepicker({
			timePicker: true,
			timePicker24Hour: true,
			timePickerIncrement: 30,
			singleDatePicker: true,
			showDropdowns: true,
			locale: {
				format: "YYYY-MM-DD HH:mm:ss"
			}
		});

		this._clipboard = new ClipboardJS(".copyBtn");

		this._clipboard.on("success", function (e) {});
		this._clipboard.on("error", function (e) {});
		this.setState({ Show: true });
	}

	componentWillUnmount() {
		this._clipboard.destroy();
	}

	onClose = () => {
		window.location.replace(location.href);
	};
	onUploadFile = event => {
		event.preventDefault();
		let files = this.filebox.current.files;
		let bankInfo = this.props.bankInfo;

		if (files.length > 0) {
			this.setState({ UploadDisabled: false });
			let formData = new FormData();
			formData.append("TransId", bankInfo.transId);
			formData.append("Receipt", files[0]);
			axios
				.post("/api/Payment/UploadReceipt", formData, {
					headers: {
						"Content-Type": "multipart/form-data"
					}
				})
				.then(
					function (response) {
						let result = response.data;
						if (result.errorCode == 0) {
							let fullPath = files[0].name;
							var startIndex =
								fullPath.indexOf("\\") >= 0
									? fullPath.lastIndexOf("\\")
									: fullPath.lastIndexOf("/");
							var filename = fullPath.substring(startIndex);
							if (
								filename.indexOf("\\") === 0 ||
								filename.indexOf("/") === 0
							) {
								filename = filename.substring(1);
							}
							this.props.bankInfo.picFileName = filename;
							this.forceUpdate();
							alert(i18n.lbl_UploadSuccess);
						} else {
							alert(result.message);
						}
					}.bind(this)
				)
				.catch(
					function (error) {
						alert(error);
					}.bind(this)
				);
		} else {
			this.setState({ UploadDisabled: true });
		}
	};
	onCheckUpload = event => {
		let files = this.filebox.current.files;
		if (files.length > 0) {
			this.setState({ UploadDisabled: false });
		} else {
			this.setState({ UploadDisabled: true });
		}
	};
	handleSubmit = event => {
		event.preventDefault();
		let valid = true;

		if (!valid) {
			this.setState({ SubmitDisabled: true });
		} else {
			this.setState({ SubmitDisabled: false });
			let bankInfo = this.props.bankInfo;
			let postData = {
				TransId: bankInfo.transId,
				DepositTime: moment($(this.dateBox.current).val()).unix(),
				RefNo: this.state.RefNo
			};
			axios
				.post("/api/Payment/SetDepositReceipt", postData)
				.then(
					function (response) {
						let result = response.data;
						if (result.errorCode == 0) {
							alert(i18n.lbl_UpdateSuccess);
							this.onClose();
						} else {
							alert(result.message);
						}
					}.bind(this)
				)
				.catch(
					function (error) {
						alert(error);
					}.bind(this)
				);
		}
	};
	EditRefNo = e => {
		this.setState({ RefNo: e.target.value });
	};
	render() {
		return (
			<div
				className="remodal-wrapper remodal-is-opened"
				style={{ display: "block" }}>
				<div className="remodal remodal-is-initialized remodal-is-opened">
					<button
						className="remodal-close"
						onClick={this.onClose}></button>
					<div className="popup-header">{i18n.lbl_Transaction}</div>
					<div className="popup-body modal-transaction">
						<p className="label">{i18n.lbl_TransactionHint}</p>
						<div className="recipe">
							<div className="item text-center">
								<a
									target="_blank"
									href={this.props.bankInfo.bankSite}>
									<img
										className="img-wallet"
										src={
											"/common/images/BankLogo/" +
											this.props.PickBankInfoOff.bankLogo
										}
										alt="wallet"
									/>
								</a>
								<p className="bank-name">
									{this.props.PickBankInfoOff.bankName}
								</p>
							</div>
							<div className="item flex">
								<div>
									<p>{i18n.lbl_BankAccountName}</p>
									<p className="text-green">
										{this.props.bankInfo.accountName}
									</p>
								</div>
								<button
									className="btn btn-secondary copyBtn"
									data-clipboard-text={
										this.props.bankInfo.accountName
									}>
									Copy
								</button>
							</div>
							<div className="item flex">
								<div>
									<p>{i18n.lbl_BankNumber}</p>
									<p className="text-green">
										{this.props.bankInfo.accountNo}
									</p>
								</div>
								<button
									className="btn btn-secondary copyBtn"
									data-clipboard-text={
										this.props.bankInfo.accountNo
									}>
									Copy
								</button>
							</div>
							<div className="item flex">
								<div>
									<p>{i18n.lbl_DepositAmount}</p>
									<p className="text-green">
										{number_format(this.props.Amount)}
									</p>
								</div>
								<button
									className="btn btn-secondary copyBtn"
									data-clipboard-text={this.props.Amount}>
									Copy
								</button>
							</div>
						</div>
						<div className="form">
							<div className="label">{i18n.lbl_AttachFile}</div>
							<div className="input-field upload-file">
								<input
									className="input-control"
									type="file"
									onChange={this.onCheckUpload.bind(this)}
									ref={this.filebox}
									accept="image/*"
								/>
								<span className="textfield__bg"></span>
								{this.props.bankInfo.picFileName != null ? (
									<a
										className="text-link"
										target="_blank"
										href={
											"/Cashier/DepositReceip.jpg?transId=" +
											this.props.bankInfo.transId +
											"&FileName=" +
											this.props.bankInfo.picFileName
										}>
										<span className="text">
											{this.props.bankInfo.picFileName}
										</span>
									</a>
								) : null}
							</div>

							<div className="button-group">
								<button
									className="btn btn-primary"
									onClick={this.onUploadFile.bind(this)}>
									{i18n.lbl_UploadFile}
								</button>
							</div>

							<div className="label">
								{i18n.lbl_DepositTime}{" "}
								<span className="text-red">
									{"(" + moment().format("Z") + ")"}
								</span>
							</div>

							<div className="input-field calendar-group">
								<input
									className="input-control"
									type="text"
									id="from-day"
									ref={this.dateBox}
									defaultValue={this.state.date}
								/>
								<span className="select-day">
									<i className="icon icon-calendar"></i>
								</span>
							</div>

							<div className="label p-t-10">{i18n.lbl_Refno}</div>
							<div className="input-field">
								<input
									className="input-control"
									type="text"
									onChange={this.EditRefNo}
									value={this.state.RefNo}
								/>
							</div>
						</div>
					</div>

					<div className="popup-footer">
						<button
							className="btn btn-secondary"
							onClick={this.onClose}>
							{i18n.lbl_Close}
						</button>
						<button
							className="btn btn-primary"
							onClick={this.handleSubmit.bind(this)}>
							{i18n.lbl_Confirm}
						</button>
					</div>
				</div>
			</div>
		);
	}
}

class SubmitLocalBank extends Component {
	constructor(props) {
		super(props);
		this.state = {
			BankInfoOffList: [],
			PickBankInfoOff: null,
			showTodModal: false,
			showBankInfoModal: false,
			transBankInfo: null
		};
	}
	componentDidMount() {
		axios
			.post("/api/Payment/GetBankInfoOffList")
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						let BankInfoOff = null;
						if (result.data.length > 0) {
							BankInfoOff = result.data[0];
						}
						this.setState({
							BankInfoOffList: result.data,
							PickBankInfoOff: BankInfoOff
						});
					} else {
						alert(result.message);
						this.onDepositOptions();
					}
				}.bind(this)
			)
			.catch(
				function (error) {
					alert(error);
					this.onDepositOptions();
				}.bind(this)
			);
	}
	onPickBankInfoOff = BankInfoOff => {
		this.setState({ PickBankInfoOff: BankInfoOff });
	};
	OnBlur = e => {
		let value = e.target.value;
		this.setState({
			Amount: value
		});
	};
	handleCheckAmount = value => {
		let status = 1;
		let message = i18n.lbl_AmountTooltip;
		let PickBankInfoOff = this.state.PickBankInfoOff;

		if (value != null && value != undefined) {
			value = IsConvertRequired === true ? value * 1000 : value;
			if (value <= 0) {
				status = 2;
				message = i18n.lbl_AmountTooltip;
			} else if (
				PickBankInfoOff.minAmount != 0 &&
				PickBankInfoOff.maxAmount != 0 &&
				(value > PickBankInfoOff.maxAmount ||
					value < PickBankInfoOff.minAmount)
			) {
				status = 2;
				message = number_format(value) + " " + i18n.lbl_NotinLimit;
			} else {
				status = 3;
				message = "";
			}
		}

		return { status: status, message: message };
	};
	handleSubmit = event => {
		event.preventDefault();
		let data = {};
		let valid = true;
		for (var i = 0; i < this.state.formArr.length; i++) {
			let dataField = this.state.formArr[i];
			dataField.onCheck();
			if (valid) {
				if (dataField.status == 2) {
					valid = false;
				}
			}
			data[dataField.name] = dataField.Value;
		}

		if (!valid) {
			this.forceUpdate();
		} else {
			let real_amount =
				IsConvertRequired === true
					? parseFloat(data["Amount"]) * 1000
					: parseFloat(data["Amount"]);
			this.setState({
				showTodModal: true,
				Amount: real_amount
			});
		}
	};
	onDepositOptions = () => {
		this.props.onDepositOptions();
	};

	onClose = () => {
		this.setState({ showTodModal: false });
	};

	onConfirm = () => {
		let real_amount =
			IsConvertRequired === true
				? Number(this.state.Amount) * 1000
				: Number(this.state.Amount);
		this.setState({ showTodModal: false });
		let data = {
			sysId: this.state.PickBankInfoOff.sysId,
			amount: real_amount
		};
		axios
			.post("/api/Payment/DepositReqWireTransfer", data)
			.then(
				function (response) {
					let result = response.data;
					if (result.errorCode == 0) {
						let bankInfo = result.data;
						bankInfo.amount = this.state.Amount;
						this.setState({
							transBankInfo: bankInfo,
							showBankInfoModal: true,
							showTodModal: false
						});
					} else {
						alert(result.message);
						this.setState({ showTodModal: true });
					}
				}.bind(this)
			)
			.catch(
				function (error) {
					alert(error);
					this.setState({ showTodModal: true });
				}.bind(this)
			);
	};
	OnSubmit = () => {
		let checked = this.handleCheckAmount(this.state.Amount);
		if (checked.status == 3) {
			this.setState({ showTodModal: true });
		} else {
			alert(checked.message);
		}
	};
	CloseTodModal = () => {
		this.setState({ showTodModal: false, showBankInfoModal: false });
	};
	render() {
		return (
			<div
				className="section options-method"
				id="optionLocalBanking"
				style={{ display: "block" }}>
				<div className="option-header">
					<span className="text-bold link-back">
						{i18n.lbl_WireTransfers}:
					</span>
					<span className="text-bold">{i18n.lbl_SelectBank}</span>
				</div>
				<div className="form">
					<div className="form-group">
						<div className="label">
							<span className="text-bold">Bank name: </span>
						</div>
						{this.state.PickBankInfoOff === null ? null : (
							<Dropdown
								BankInfoOffList={this.state.BankInfoOffList}
								PickBankInfoOff={this.state.PickBankInfoOff}
								onPickBankInfoOff={this.onPickBankInfoOff.bind(
									this
								)}
							/>
						)}
						<div className="label">
							<span className="text-bold">
								{i18n.lbl_Amount}:{" "}
							</span>
						</div>
						<div className="input-field">
							<input
								className="input-control text-green text-right"
								type="number"
								pattern="[0-9]*"
								onBlur={this.OnBlur}
							/>
							{IsConvertRequired ? (
								<span className="thousands text-green text-bold">
									('000)
								</span>
							) : null}
						</div>
					</div>

					<div className="button-group">
						<button
							className="btn btn-primary"
							id="btn-back"
							onClick={this.props.ClickBack}>
							{i18n.lbl_Back}
						</button>
						<button
							className="btn btn-secondary"
							onClick={this.OnSubmit}>
							{i18n.lbl_Submit}
						</button>
					</div>
				</div>

				<div
					className="remodal-overlay remodal-is-opened"
					style={{
						display:
							this.state.showTodModal ||
							this.state.showBankInfoModal === true
								? "block"
								: "none"
					}}></div>
				<div
					className="remodal-wrapper remodal-is-opened"
					style={{
						display:
							this.state.showTodModal === true ? "block" : "none"
					}}>
					<div className="remodal remodal-is-initialized remodal-is-opened">
						<button
							className="remodal-close"
							onClick={this.CloseTodModal}></button>
						<div className="popup-header">
							{i18n.lbl_ImportantNote}
						</div>
						<div className="popup-body">
							<p>{i18n.lbl_ConfirmDeposit}</p>
							{this.state.PickBankInfoOff === null ? null : (
								<img
									className="img-wallet"
									src={
										"/common/images/BankLogo/" +
										this.state.PickBankInfoOff.bankLogo
									}
									alt="wallet"
								/>
							)}
							<div className="confirm-value">
								<span>{i18n.lbl_DepositAmount}</span>
								<span className="text-bold value">
									{number_format(
										IsConvertRequired === true
											? Number(this.state.Amount) * 1000
											: Number(this.state.Amount)
									)}
								</span>
							</div>
						</div>

						<div className="popup-footer">
							<button
								className="btn btn-primary"
								onClick={this.CloseTodModal}>
								{i18n.lbl_Back}
							</button>
							<button
								className="btn btn-primary"
								onClick={this.onConfirm}>
								{i18n.lbl_Submit}
							</button>
						</div>
					</div>
				</div>
				{this.state.showBankInfoModal ? (
					<BankInfoModal
						bankInfo={this.state.transBankInfo}
						PickBankInfoOff={this.state.PickBankInfoOff}
						Amount={
							IsConvertRequired === true
								? this.state.Amount * 1000
								: this.state.Amount
						}
					/>
				) : null}
			</div>
		);
	}
}

export default class LocalBank extends Component {
	constructor(props) {
		super(props);
		this.state = { LocalBank: true, subPage: false };
	}
	componentDidMount() {
		this.getLocalBank();
	}
	getLocalBank = () => {
		axios.post("/api/Payment/GetBankInfoOffList").then(
			function (response) {
				let result = response.data;
				if (result.errorCode == 0) {
					let localBank = false;
					if (result.data.length > 0) {
						localBank = true;
					}
					this.setState({ LocalBank: localBank });
				}
			}.bind(this)
		);
	};
	onClickLocalBank = () => {
		this.setState({ subPage: true });
		this.props.hangleSubPage(true, 1);
	};
	ClickBack = () => {
		this.setState({ subPage: false });
		this.props.hangleSubPage(false, null);
	};
	render() {
		return this.state.LocalBank ? (
			this.state.subPage ? (
				<SubmitLocalBank ClickBack={this.ClickBack} />
			) : (
				<>
					<button
						className="option-header toggle-section toggle-btn"
						data-toggle-content="section-local-bank">
						<span className="text-bold">
							{i18n.lbl_WireTransfers} -
						</span>
						<span className="text-italic">
							{i18n.lbl_CreditTime}: {i18n.lbl_5mins}
						</span>
					</button>
					<div className="section-content" id="section-local-bank">
						<div className="option-content">
							<div
								className="card js-local-banking"
								onClick={this.onClickLocalBank}>
								<div className="option-item">
									<img src="/common/images/Local_Bank.png" />
									<div className="value">
										<p>{i18n.lbl_WireTransfers}</p>
									</div>
								</div>
							</div>
						</div>
					</div>
				</>
			)
		) : (
			<></>
		);
	}
}

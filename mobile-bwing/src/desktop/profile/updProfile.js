import React, { createRef, Component } from "react";
import ReactDOM from "react-dom";
import areaCode from "../../common/areaCode";
import WebAPI from "../../service/api/base";
import moment from "moment";

/* eslint-disable */
const InputField = ({
	columnTitle,
	value,
	onChange,
	readOnly,
	type,
	error
}) => (
	<>
		<div className="label text-bold">{columnTitle}: </div>
		<div className="input-field">
			<input
				className="input-control invalid"
				type={type ? type : "text"}
				value={value}
				onChange={onChange}
				readOnly={readOnly === true}
			/>
			{error?.status !== true ? (
				<div className="invalid-mess">{error?.msg}</div>
			) : null}
			{readOnly ? <i className="icon icon-lock"></i> : null}
		</div>
	</>
);

const Panel = () => {
	return <UpdateProfile />;
};

const readOnlyDict =
	SetStatus == "1"
		? {
				accountName: false,
				brithday: false,
				phone: false,
				country: true
		  }
		: SetStatus == "2"
		? {
				accountName: false,
				brithday: true,
				phone: true,
				country: true
		  }
		: {
				accountName: true,
				brithday: false,
				phone: false,
				country: true
		  };

const ModalType = {
	confirm: 1,
	goToDeposit: 2
};

class UpdateProfile extends Component {
	constructor(props) {
		super(props);
		this.state = {
			AccountName: custdata.AccountName,
			PhoneNo: custdata.PhoneNo,
			modalType: ModalType.confirm,
			error: {
				AccountName: { status: true, msg: "" },
				PhoneNo: { status: true, msg: "" },
				Birthday: { status: true, msg: "" }
			},
			lockAll: false,
			Email: custdata.Email ?? ""
		};
		this.phone_prefix = `${areaCode[perCountry].AreaCode}　`;
		this.dateRef = createRef();
	}

	componentDidMount = () => {
		if (!readOnlyDict.brithday) {
			$(this.dateRef.current).daterangepicker({
				singleDatePicker: true,
				showDropdowns: true,
				locale: {
					format: "DD/MM/YYYY"
				}
			});
		}
	};

	checkEmailIsUnique = async email => {
		try {
			if (!custdata.email && email && email.length > 0) {
				const res = await WebAPI().post("/api/Site/ExistsEmail", {
					Email: email
				});

				let data = res.data;

				if (data.errorCode === 0) {
					if (data.data === true) {
						return { checked: false, msg: i18n["msg_MailExisted"] };
					} else if (data.data === false) {
						return { checked: true, msg: "" };
					}
				} else {
					return { checked: false, msg: "" };
				}
			}
		} catch (err) {
			return { checked: false, msg: "" };
		}
	};

	CheckDuplicateEmail = async email => {
		let checkResult = true;

		let checkTip = "";
		let emailRegex = /^\w+((-\w+)|(\.\w+))*@[A-Za-z0-9]+((\.|-)[A-Za-z0-9]+)*\.[A-Za-z]+$/;

		if (!email) {
			checkResult = false;
		}

		if (!custdata.Email && email === "") {
			checkResult = false;
			checkTip = i18n["lbl_EmailRequired"];
		} else if (
			!custdata.email &&
			email.length > 0 &&
			!emailRegex.test(email)
		) {
			checkResult = false;
			checkTip = i18n["lbl_EmailFormatError"];
		} else if (
			!custdata.Email &&
			email.length > 0 &&
			emailRegex.test(email)
		) {
			let result = await this.checkEmailIsUnique(email);
			checkResult = result.checked;
			checkTip = result.msg;
		}

		return { checked: checkResult, msg: checkTip };
	};

	EditField = async (colName, e) => {
		let value = e.target.value;
		let checked = true;
		let msg = "";

		if (colName === "PhoneNo") {
			if (!value.startsWith(this.phone_prefix)) {
				checked = false;
			}

			if (checked) {
				let tmpValue = value.replace(this.phone_prefix, "");
				const regex = RegExp(/^[0-9]*$/);
				checked = regex.test(tmpValue);
			}
		} else if (colName === "Email") {
			let result = await this.CheckDuplicateEmail(value);
			checked = result.checked;
			msg = result.msg;
		}

		if (checked) {
			this.setState(prevState => ({
				[colName]: value,
				error: {
					...prevState.error,
					[colName]: {
						status: true,
						msg: ""
					}
				}
			}));
		} else {
			this.setState(prevState => ({
				[colName]: value,
				error: {
					...prevState.error,
					[colName]: {
						status: false,
						msg: msg
					}
				}
			}));
		}
	};

	ClickUpdate = async () => {
		let error = {
			AccountName: { status: true, msg: "" },
			PhoneNo: { status: true, msg: "" },
			Birthday: { status: true, msg: "" },
			Email: { status: true, msg: "" }
		};

		let checked = true;
		if (!readOnlyDict.accountName && !this.state.AccountName) {
			checked = false;
			error.AccountName.status = false;
			error.AccountName.msg = i18n["msg_EnterRealName"];
		}

		if (!readOnlyDict.brithday && !$(this.dateRef.current).val()) {
			checked = false;
			error.Birthday.status = false;
			error.Birthday.msg = i18n["msg_SelectBirthDay"];
		}

		if (!readOnlyDict.phone) {
			let tmpValue = this.state.PhoneNo.replace(this.phone_prefix, "");
			const regex = RegExp(/^[0-9]*$/);
			let phone_checked = tmpValue !== "" && regex.test(tmpValue);
			error.PhoneNo.status = phone_checked;
			if (!phone_checked) {
				error.PhoneNo.msg = i18n["msg_EnterPhoneNumber"];
			}
			checked = checked && phone_checked;
		}

		let checkEmail = await this.CheckDuplicateEmail(this.state.Email);
		if (!checkEmail.checked) {
			checked = false;
			error.Email.status = false;
			error.Email.msg = checkEmail.msg;
		}

		if (checked) {
			var inst = $("[data-remodal-id=modal-updateProfile]").remodal();
			inst.open();
		} else {
			this.setState({ error: error });
		}
	};

	ExecuteUpdate = () => {
		let b_date = readOnlyDict.brithday
			? [
					custdata.Birthday.day,
					custdata.Birthday.month,
					custdata.Birthday.year
			  ]
			: $(this.dateRef.current).val().split("/");

		let data = {
			AccountName: this.state.AccountName,
			Birth_d: b_date[0],
			Birth_m: b_date[1],
			Birth_y: b_date[2],
			CountryCode: perCountry,
			AreaCode: areaCode[perCountry].AreaCode,
			PhoneNo: this.state.PhoneNo.replace(this.phone_prefix, ""),
			Email: this.state.Email
		};

		WebAPI()
			.post("/api/AccountStrict/SetProfileV2", data)
			.then(
				response => {
					let res = response.data;
					if (res.errorCode == 0) {
						this.setState({
							modalType: ModalType.goToDeposit,
							lockAll: true
						});
					} else {
						switch (res.errorCode) {
							case 21:
								this.setState(prevState => ({
									error: {
										...prevState.error,
										PhoneNo: {
											status: false,
											msg: res.message
										}
									}
								}));
								break;
							default:
								alert(res.message);
								break;
						}
						var inst = $(
							"[data-remodal-id=modal-updateProfile]"
						).remodal();
						inst.close();
					}
				},
				err => {
					alert(err);
				}
			);
	};

	render() {
		let title =
			this.state.modalType === ModalType.confirm
				? i18n["lbl_Confirm"]
				: "";
		let btnBar =
			this.state.modalType === ModalType.confirm ? (
				<ConfirmBtnBar ExecuteUpdate={this.ExecuteUpdate} />
			) : (
				<GotoDepositBtnBar />
			);
		let modalContent =
			this.state.modalType === ModalType.confirm ? (
				<ConfirmContent accountName={this.state.AccountName} />
			) : (
				<GotoDepositContent />
			);

		let defaultBirth = custdata.Birthday.day
			? `${custdata.Birthday.day}/${custdata.Birthday.month}/${custdata.Birthday.year}`
			: moment().add(-21, "year").format("DD/MM/YYYY");
		return (
			<>
				<div className="title">{i18n.lbl_Profile}</div>
				<div className="content">
					<div
						id="form-edit"
						className="form hide"
						style={{ display: "block" }}>
						<div className="form-horizontal">
							<div className="form-group">
								<InputField
									columnTitle={i18n["lbl_RealName"]}
									value={this.state.AccountName}
									readOnly={
										this.state.lockAll ||
										readOnlyDict.accountName
									}
									onChange={this.EditField.bind(
										this,
										"AccountName"
									)}
									error={this.state.error.AccountName}
								/>
							</div>

							{this.state.lockAll || readOnlyDict.brithday ? (
								<div className="form-group">
									<InputField
										columnTitle={i18n["lbl_Birthday"]}
										value={defaultBirth}
										readOnly={true}
										onChange={this.EditField.bind(
											this,
											"Birthday"
										)}
									/>
								</div>
							) : (
								<div className="form-group">
									<div className="label">
										{i18n["lbl_Birthday"]}:{" "}
									</div>
									<div className="input-field calendar-group single-calendar">
										<input
											className="input-control"
											type="text"
											ref={this.dateRef}
											readOnly={readOnlyDict.brithday}
											defaultValue={defaultBirth}
										/>
										{this.state.error.Birthday?.status !==
										true ? (
											<div className="invalid-mess">
												{this.state.error.Birthday?.msg}
											</div>
										) : null}
										<span className="select-day">
											<i className="icon icon-calendar"></i>
										</span>
									</div>
								</div>
							)}
						</div>

						<div className="form-horizontal">
							<div className="form-group">
								<InputField
									type={"phone"}
									columnTitle={i18n["lbl_Phone"]}
									value={`${
										this.phone_prefix
									}${this.state.PhoneNo.replace(
										this.phone_prefix,
										""
									)}`}
									readOnly={
										this.state.lockAll || readOnlyDict.phone
									}
									onChange={this.EditField.bind(
										this,
										"PhoneNo"
									)}
									error={this.state.error.PhoneNo}
								/>
							</div>
							<div className="form-group">
								<InputField
									columnTitle={i18n["lbl_Country"]}
									value={GetDefaultCountry().displayText}
									readOnly={
										this.state.lockAll ||
										readOnlyDict.country
									}
								/>
							</div>
						</div>

						<div className="form-horizontal">
							<div className="form-group">
								<InputField
									columnTitle={i18n["lbl_Email"]}
									value={this.state.Email}
									readOnly={Boolean(custdata.Email)}
									onChange={this.EditField.bind(
										this,
										"Email"
									)}
									error={this.state.error.Email}
								/>
							</div>
						</div>

						{this.state.lockAll ? null : (
							<div className="button-group">
								<button
									className="btn btn-primary"
									onClick={this.ClickUpdate}>
									{i18n["lbl_Update"]}
								</button>
							</div>
						)}
					</div>
				</div>

				<div
					className="remodal"
					data-remodal-id="modal-updateProfile"
					data-remodal-options="hashTracking: false, closeOnOutsideClick: false">
					<div className="popup-header">{title}</div>
					<div className="popup-body">{modalContent}</div>
					<div className="popup-footer">{btnBar}</div>
				</div>
			</>
		);
	}
}

const ConfirmContent = ({ accountName }) => {
	return (
		<>
			<p>{accountName}</p>
			<p>{i18n["msg_ConfirmRealNameAlert"]}</p>
		</>
	);
};

const GotoDepositContent = () => {
	return (
		<>
			<p>{i18n["msg_Updatesuccessful"]}</p>
			<p>{i18n["msg_GoToDepositPage"]}</p>
		</>
	);
};

const ConfirmBtnBar = ({ ExecuteUpdate }) => {
	return (
		<>
			<button className="btn btn-login" data-remodal-action="close">
				{i18n["lbl_ReModify"]}
			</button>
			<button className="btn btn-login" onClick={ExecuteUpdate}>
				{i18n["lbl_ConfirmAndUpdate"]}
			</button>
		</>
	);
};

const GotoDepositBtnBar = () => {
	const RedirectToDeposit = () => {
		window.location.href = `/${SiteLanguage}/Cashier/Deposit`;
	};
	return (
		<>
			<button className="btn btn-login" data-remodal-action="close">
				{i18n["lbl_Later"]}
			</button>
			<button className="btn btn-login" onClick={RedirectToDeposit}>
				{i18n["lbl_DepositNow"]}
			</button>
		</>
	);
};

const GetDefaultCountry = () => {
	let countryInfo = Country.find(x => x.Code === perCountry);
	return { value: countryInfo.Code, displayText: countryInfo.CountryName };
};

/* eslint-enable */
export default Panel;

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<Panel />, document.getElementById("content"));
}

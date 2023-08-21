import { DropdownBox } from "../components/dropDownBox";
import areaCode from "../../../commJS/areaCode";
import { InputBox } from "../components/inputBox";
import { handleCheckPhoneNo } from "../vaildValue";
import * as Loading from "../../../commJS/Loading.jsx";
import { PhoneNumber } from "../components/phoneNumber";
import { ErrorTips } from "../components/errorTips";
import {
	Modal,
	ConfirmRealName,
	ConfirmGoToDepositPage
} from "../components/modal";
import { CustInfo } from "../NormalProfile//CustInfo";

(function ($, React, ReactDOM, i18n, axios) {
	var custInfo = {};

	const elemType = {
		inputBox: "inputBox",
		birthday: "birthday",
		phone: "phone",
		dropdownBox: "dropdownBox",
		countryBox: "countryBox"
	};

	const columnDefs = () => {
		let yearOptions = [];
		let startYear = GetYear();
		for (let i = startYear; i >= startYear - 80; i--) {
			yearOptions.push({ value: i, displayText: i });
		}

		return [
			{
				dataColumn: "AccountName",
				columnTitle: i18n.lbl_RealName,
				placeHolder: i18n.lbl_RemindForName,
				elemType: elemType.inputBox,
				readOnly: SetStatus == 3 || SetStatus == 4
			},
			{
				dataColumn: "Birthday",
				columnTitle: i18n.lbl_Birthday,
				day: {
					dataColumn: "day"
				},
				month: {
					dataColumn: "month",
					options: [
						{ value: 0, displayText: i18n.lbl_Month1 },
						{ value: 1, displayText: i18n.lbl_Month2 },
						{ value: 2, displayText: i18n.lbl_Month3 },
						{ value: 3, displayText: i18n.lbl_Month4 },
						{ value: 4, displayText: i18n.lbl_Month5 },
						{ value: 5, displayText: i18n.lbl_Month6 },
						{ value: 6, displayText: i18n.lbl_Month7 },
						{ value: 7, displayText: i18n.lbl_Month8 },
						{ value: 8, displayText: i18n.lbl_Month9 },
						{ value: 9, displayText: i18n.lbl_Month10 },
						{ value: 10, displayText: i18n.lbl_Month11 },
						{ value: 11, displayText: i18n.lbl_Month12 }
					]
				},
				year: {
					dataColumn: "year",
					options: yearOptions
				},
				elemType: elemType.birthday,
				readOnly: SetStatus == 2
			},
			{
				dataColumn: "Country",
				columnTitle: i18n.lbl_Country,
				elemType: elemType.countryBox
			},
			{
				dataColumn: "PhoneNo",
				columnTitle: i18n.lbl_Phone,
				elemType: elemType.phone,
				regex: /^[0-9]*$/,
				readOnly: SetStatus == 2 || SetStatus == 4
			}
		];
	};

	const vaildFunc = {
		PhoneNo: {
			func: handleCheckPhoneNo,
			bindingValue: {
				dataColumn: "Country"
			}
		}
	};

	const Panel = () => {
		const [stateCustInfo, updateCustInfo] = React.useState(null);

		const [stateModal, updateModal] = React.useState({
			title: "",
			isOpen: false,
			content: "",
			btnBar: ""
		});

		const OpenModal = (title, content, btnBar) => {
			updateModal({
				title: title,
				isOpen: true,
				content: content,
				btnBar: btnBar
			});
		};

		const CloseModal = otherInfo => {
			updateModal({ isOpen: false, otherInfo: otherInfo });
		};

		return (
			<>
				{stateModal.isOpen === true ? (
					<Modal
						isOpen={stateModal.isOpen}
						title={stateModal.title}
						content={stateModal.content}
						btnBar={stateModal.btnBar}
					/>
				) : null}
				<div className="heading" role="desktop-only">
					<span className="text">{i18n.lbl_Profile}</span>
				</div>
				{stateCustInfo === null ? (
					<FormContent
						ColumnDefs={columnDefs()}
						DefaultCountry={GetDefaultCountry()}
						OpenModal={OpenModal}
						CloseModal={CloseModal}
						UpdateCustInfo={updateCustInfo}
					/>
				) : (
					<CustInfo CustInfo={stateCustInfo} />
				)}
			</>
		);
	};

	const GetDefaultCountry = () => {
		let countryInfo = Country.find(x => x.Code === perCountry);
		return {
			value: countryInfo.Code,
			displayText: countryInfo.CountryName
		};
	};

	const FormContent = props => {
		let columns = props.ColumnDefs;

		const [stateDatas, updateDatas] = React.useState(
			SetStatus == 1
				? {
						AccountName: "",
						Birthday: {},
						Country: props.DefaultCountry
				  }
				: SetStatus == 2
				? {
						AccountName: "",
						Birthday: {
							day: {
								value: custdata.Birthday.day,
								displayText: custdata.Birthday.day
							},
							month: {
								value: Number(custdata.Birthday.month) - 1,
								displayText:
									i18n["lbl_Month" + custdata.Birthday.month]
							},
							year: {
								value: custdata.Birthday.year,
								displayText: custdata.Birthday.year
							}
						},
						Country: props.DefaultCountry,
						PhoneNo: custdata.PhoneNo
				  }
				: SetStatus == 3
				? {
						AccountName: custdata.AccountName,
						Birthday: {},
						Country: props.DefaultCountry
				  }
				: {
						AccountName: custdata.AccountName,
						Birthday: {},
						Country: props.DefaultCountry,
						PhoneNo: custdata.PhoneNo
				  }
		);

		const [stateErrors, updateErrors] = React.useState({
			Birthday: ""
		});

		const EditField = (value, dataColumn, bindingCol) => {
			let check = true;
			let message = "";

			if (value !== "" && vaildFunc[dataColumn]) {
				if (
					vaildFunc[dataColumn].bindingValue &&
					stateDatas[vaildFunc[dataColumn].bindingCol]
				) {
					let result = vaildFunc[dataColumn].func(
						value,
						vaildFunc[dataColumn].bindingCol
							? stateDatas[vaildFunc[dataColumn].bindingCol].value
							: null
					);
					check = result.status === 0;
					message = result.message;
				}
			}

			if (check) {
				if (stateErrors[dataColumn] || stateErrors[bindingCol]) {
					let tmp = { ...stateErrors };
					if (tmp[dataColumn]) {
						tmp[dataColumn] = "";
					}
					if (tmp[bindingCol]) {
						tmp[bindingCol] = "";
					}
					updateErrors({ ...tmp });
				}

				updateDatas(
					bindingCol
						? {
								...stateDatas,
								[bindingCol]: {
									...stateDatas[bindingCol],
									[dataColumn]: value
								}
						  }
						: {
								...stateDatas,
								[dataColumn]: value
						  }
				);
			}
		};

		const ExecuteUpdate = () => {
			Loading.OnLoading();
			props.CloseModal();
			let data = {
				AccountName: stateDatas.AccountName,
				Birth_d: `${stateDatas.Birthday.day.value}`,
				Birth_m: `${stateDatas.Birthday.month.value + 1}`,
				Birth_y: `${stateDatas.Birthday.year.value}`,
				CountryCode: stateDatas.Country.value,
				AreaCode: areaCode[stateDatas.Country.value].AreaCode,
				PhoneNo: stateDatas.PhoneNo
			};

			axios
				.post("/api/AccountStrict/SetProfile", data)
				.then(
					function (response) {
						Loading.UnLoading();
						let result = response.data;
						if (result.errorCode == 0) {
							let { langCode, ...rest } = result.data;

							props.UpdateCustInfo({
								UserName: rest.userName,
								Email: rest.email,
								AccountName: rest.accountName,
								Birth_d: rest.birth_d,
								Birth_m: rest.birth_m,
								Birth_y: rest.birth_y,
								RegTime: rest.regTime,
								LastLogin: rest.lastLogin,
								Country: rest.country,
								PhoneNo: rest.phoneNo
							});

							let modalInfo = ConfirmGoToDepositPage({
								CloseModal: props.CloseModal,
								LangCode: langCode
							});
							props.OpenModal(
								modalInfo.title,
								modalInfo.content,
								modalInfo.btnBar
							);
						} else {
							switch (result.errorCode) {
								case 21:
									updateErrors({
										...stateErrors,
										PhoneNo: result.message
									});
									break;
								default:
									alert(result.message);
									break;
							}

							props.CloseModal();
						}
					}.bind(this)
				)
				.catch(
					function (error) {
						Loading.UnLoading();
						alert(error);
						props.CloseModal();
					}.bind(this)
				);
		};

		const ClickSave = async () => {
			event.preventDefault();

			let check_phone =
				stateDatas.PhoneNo && stateDatas.Country
					? handleCheckPhoneNo(
							stateDatas.PhoneNo,
							stateDatas.Country.value
					  )
					: false;
			let valid = true;

			let errors = { ...stateErrors };
			if (
				!stateDatas.Birthday.year ||
				!stateDatas.Birthday.month ||
				!stateDatas.Birthday.day
			) {
				valid = false;
				errors = { ...errors, Birthday: i18n.msg_SelectBirthDay };
			}

			if (!stateDatas.AccountName) {
				valid = false;
				errors = { ...errors, AccountName: i18n.msg_EnterRealName };
			}

			if (!stateDatas.PhoneNo) {
				valid = false;
				errors = { ...errors, PhoneNo: i18n.msg_EnterPhoneNumber };
			}

			valid = valid ? check_phone.status === 0 : valid;

			if (valid) {
				let modalInfo = ConfirmRealName({
					CloseModal: props.CloseModal,
					ConfirmProfile: ExecuteUpdate,
					AccountName: stateDatas.AccountName
				});

				props.OpenModal(
					modalInfo.title,
					modalInfo.content,
					modalInfo.btnBar
				);
			} else {
				Loading.UnLoading();
				updateErrors({
					...errors
				});
			}
		};

		let rows = columns.map((item, idx) => {
			let key = `form-field-${idx}`;
			let result = <div key={key} />;
			switch (item.elemType) {
				case elemType.inputBox:
					result = (
						<InputBox
							key={key}
							columnTitle={item.columnTitle}
							placeHolder={item.placeHolder}
							dataColumn={item.dataColumn}
							onChange={EditField}
							value={
								stateDatas[item.dataColumn] === undefined ||
								stateDatas[item.dataColumn] === null
									? ""
									: stateDatas[item.dataColumn]
							}
							ErrorMsg={stateErrors[item.dataColumn]}
							ReadOnly={item.readOnly}
						/>
					);
					break;

				case elemType.birthday:
					let dateOptions = [];
					let today = new Date();
					let year = stateDatas.Birthday.year
						? stateDatas.Birthday.year.value
						: today.getFullYear();
					let month = stateDatas.Birthday.month
						? stateDatas.Birthday.month.value
						: today.getMonth();
					let days = GetDayOfMonth(year, month + 1);
					if (
						stateDatas.Birthday.day &&
						stateDatas.Birthday.day.value > days
					) {
						updateDatas({
							...stateDatas,
							Birthday: {
								...stateDatas.Birthday,
								day: {}
							}
						});
					}

					for (let i = 1; i <= days; i++) {
						dateOptions.push({ value: i, displayText: i });
					}

					result = (
						<Birthday
							key={key}
							columnTitle={item.columnTitle}
							DataColumn={item.dataColumn}
							OnSelectItem={EditField}
							Day={{
								DataColumn: item.day.dataColumn,
								SelectedItem: stateDatas[item.dataColumn].day,
								Options: dateOptions
							}}
							Month={{
								DataColumn: item.month.dataColumn,
								SelectedItem: stateDatas[item.dataColumn].month,
								Options: item.month.options
							}}
							Year={{
								DataColumn: item.year.dataColumn,
								SelectedItem: stateDatas[item.dataColumn].year,
								Options: item.year.options
							}}
							ErrorMsg={stateErrors[item.dataColumn]}
							ReadOnly={item.readOnly}
						/>
					);
					break;

				case elemType.phone:
					result = (
						<PhoneNumber
							key={key}
							dataColumn={item.dataColumn}
							columnTitle={item.columnTitle}
							areaCode={
								stateDatas.Country
									? areaCode[stateDatas.Country.value]
											.AreaCode
									: ""
							}
							onChange={EditField}
							regex={item.regex}
							value={
								stateDatas[item.dataColumn] === undefined ||
								stateDatas[item.dataColumn] === null
									? ""
									: stateDatas[item.dataColumn]
							}
							ErrorMsg={stateErrors[item.dataColumn]}
							ReadOnly={item.readOnly}
						/>
					);
					break;

				case elemType.countryBox:
					result = (
						<CountryBox
							key={key}
							ColumnTitle={item.columnTitle}
							Country={stateDatas.Country.displayText}
						/>
					);
					break;

				default:
					break;
			}

			return result;
		});

		return (
			<form className="form form--list" onSubmit={ClickSave}>
				{rows}
				<div className="btn-group">
					<button className="btn btn-secondary">
						<div className="btn__inner">
							<span className="text">{i18n.lbl_Update}</span>
						</div>
					</button>
				</div>
			</form>
		);
	};

	const Birthday = props => {
		return (
			<div className="form-group">
				<label className="form-title">{props.columnTitle}</label>
				<div
					className="form-content form-content--birthday"
					data-status="info">
					<DropdownBox
						{...props.Day}
						BindingValue={props.DataColumn}
						OnSelectItem={props.OnSelectItem}
						ReadOnly={props.ReadOnly}
					/>
					<DropdownBox
						{...props.Month}
						BindingValue={props.DataColumn}
						OnSelectItem={props.OnSelectItem}
						ReadOnly={props.ReadOnly}
					/>
					<DropdownBox
						{...props.Year}
						BindingValue={props.DataColumn}
						OnSelectItem={props.OnSelectItem}
						ReadOnly={props.ReadOnly}
					/>
					<ErrorTips ErrorMsg={props.ErrorMsg} />
				</div>
			</div>
		);
	};

	const GetYear = () => {
		return new Date().getFullYear() - 21;
	};

	const GetDayOfMonth = (year, month) => {
		let d = new Date(year, month, 0);
		return d.getDate();
	};

	const CountryBox = props => {
		return (
			<div className="form-group">
				<label className="form-title">{props.ColumnTitle}</label>
				<div className="form-content" data-status="info">
					<div className="dropdown dropdown-default">
						<a className="btn">
							<span className="text">{props.Country}</span>
						</a>
					</div>
				</div>
			</div>
		);
	};

	ReactDOM.render(<Panel />, document.getElementById("Profile"));
})(jQuery, React, ReactDOM, i18n, axios, moment);

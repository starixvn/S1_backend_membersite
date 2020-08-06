import React, { Component, createRef } from "react";
import ReactDOM from "react-dom";
import Pagination from "../../common/pagination";
import WebAPI from "../../service/api/base";
import History from "./deposit.history";
import { QuickPicker, QuickPickDefs } from "./dateQuickPicker";
import { BankInfoModal } from "../cashier/deposit.localBank";

/* eslint-disable */
class Panel extends Component {
	constructor(props) {
		super(props);
		this.startDateRef = createRef();
		this.endDateRef = createRef();
		this.state = {
			StartDate: moment().add(-30, "day").format("YYYY-MM-DD"),
			EndDate: moment().format("YYYY-MM-DD"),
			GetData: false,
			QuickPick: QuickPickDefs.Today,
			showBankInfoModal: false,
			transBankInfo: null,
			PickBankInfoOff: null,
			Amount: null
		};
	}
	componentDidMount = () => {
		$(this.startDateRef.current).daterangepicker({
			singleDatePicker: true,
			showDropdowns: true,
			locale: {
				format: "YYYY-MM-DD"
			}
		});

		$(this.endDateRef.current).daterangepicker({
			singleDatePicker: true,
			showDropdowns: true,
			locale: {
				format: "YYYY-MM-DD"
			}
		});
	};
	GetDepositInfoHistory = pageNumber => {
		if (this.state.EndDate == "" || this.state.EndDate == "") {
			alert(i18n.lbl_DateMissing);
			return;
		}

		let endDate =
			moment($(this.endDateRef.current).val()).add(1, "days").unix() - 1;
		let data = {
			StartDate: moment($(this.startDateRef.current).val()).unix(),
			EndDate: endDate,
			PageNumber: pageNumber
		};

		WebAPI()
			.post("/api/Payment/GetDepositInfoHistory", data)
			.then(
				response => {
					let result = response.data;
					if (result.errorCode == 0) {
						this.setState({
							PageNumber: result.pageNumber,
							TotalPages: result.totalPages,
							DepositInfoHistory: result.data,
							GetData: true
						});
					} else {
						alert(result.message);
					}
				},
				error => {
					alert(error);
				}
			);
	};
	SetDate = (dataColumn, e) => {
		this.setState({ [dataColumn]: e.target.value });
	};
	QuickPickOnSelect = (v, datacolumn) => {
		let StartDate = "";
		let EndDate = "";

		switch (v.value) {
			case QuickPickDefs.Today.value:
				StartDate = moment().format("YYYY-MM-DD");
				EndDate = moment().format("YYYY-MM-DD");
				break;

			case QuickPickDefs.YesterDay.value:
				StartDate = moment().add(-1, "days").format("YYYY-MM-DD");
				EndDate = moment().add(-1, "days").format("YYYY-MM-DD");
				break;

			case QuickPickDefs.LastWeek.value:
				StartDate = moment()
					.subtract(1, "weeks")
					.startOf("week")
					.format("YYYY-MM-DD");
				EndDate = moment()
					.subtract(1, "weeks")
					.endOf("week")
					.format("YYYY-MM-DD");

				break;

			case QuickPickDefs.ThisWeek.value:
				StartDate = moment()
					.subtract(1, "weeks")
					.endOf("week")
					.add(1, "days")
					.format("YYYY-MM-DD");
				EndDate = moment().format("YYYY-MM-DD");
				break;

			case QuickPickDefs.ThisMonth.value:
				StartDate = moment()
					.subtract(1, "months")
					.endOf("month")
					.add(1, "days")
					.format("YYYY-MM-DD");
				EndDate = moment().format("YYYY-MM-DD");
				break;

			case QuickPickDefs.LastMonth.value:
				StartDate = moment()
					.subtract(1, "months")
					.startOf("month")
					.format("YYYY-MM-DD");
				EndDate = moment()
					.subtract(1, "months")
					.endOf("month")
					.format("YYYY-MM-DD");
				break;

			default:
				StartDate = stateFilter.StartDate;
				EndDate = stateFilter.EndDate;
				break;
		}

		this.setState({
			StartDate: StartDate,
			EndDate: EndDate,
			QuickPick: v
		});
	};
	onCancel = transId => {
		if (confirm(i18n.lbl_CancelConfirm)) {
			axios
				.post("/api/Payment/SetDepositCancel", { TransId: transId })
				.then(
					function (response) {
						let result = response.data;
						if (result.errorCode == 0) {
							let newData = [...this.state.DepositInfoHistory];
							let find_idx = newData.findIndex(
								x => x.transId === transId
							);
							newData[find_idx].status = 5;
							this.setState({ DepositInfoHistory: newData });
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
	onShowBankModal = datas => {
		this.setState({
			showBankInfoModal: true,
			transBankInfo: datas,
			PickBankInfoOff: datas,
			Amount: datas.amount
		});
	};
	render() {
		return (
			<>
				<div className="title">{i18n.lbl_DepositHistory}</div>
				<div className="content announcement-block">
					<div className="form calendar-filter p-t-20">
						<QuickPicker
							QuickPickOnSelect={this.QuickPickOnSelect}
							QuickPick={this.state.QuickPick}
						/>
						<div className="calendar-group form-group">
							<span className="label">{i18n["lbl_From"]}: </span>
							<div className="input-field">
								<input
									className="input-control"
									type="text"
									ref={this.startDateRef}
									value={this.state.StartDate}
									onChange={this.SetDate.bind(
										this,
										"StartDate"
									)}
								/>
								<span className="select-day">
									<i className="icon icon-calendar"></i>
								</span>
							</div>
						</div>
						<div className="calendar-group form-group">
							<span className="label">{i18n["lbl_To"]}: </span>
							<div className="input-field">
								<input
									className="input-control"
									type="text"
									ref={this.endDateRef}
									value={this.state.EndDate}
									onChange={this.SetDate.bind(
										this,
										"EndDate"
									)}
								/>
								<span className="select-day">
									<i className="icon icon-calendar"></i>
								</span>
							</div>
						</div>
						<div className="button-group">
							<button
								className="btn btn-primary"
								onClick={this.GetDepositInfoHistory.bind(
									this,
									1
								)}>
								{i18n["lbl_ViewHistory"]}
							</button>
						</div>
					</div>

					{this.state.GetData ? (
						<History
							Data={this.state.DepositInfoHistory}
							onCancel={this.onCancel}
							onShowBankModal={this.onShowBankModal}
						/>
					) : null}
					<Pagination
						PageNumber={this.state.PageNumber}
						TotalPages={this.state.TotalPages}
						onPaging={this.GetDepositInfoHistory}
					/>
				</div>
				{this.state.showBankInfoModal ? (
					<BankInfoModal
						bankInfo={this.state.transBankInfo}
						PickBankInfoOff={this.state.PickBankInfoOff}
						Amount={this.state.Amount}
					/>
				) : null}
			</>
		);
	}
}

/* eslint-enable */
export default Panel;

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<Panel />, document.getElementById("content"));
}

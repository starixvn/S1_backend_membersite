import React, { Component, createRef } from "react";
import ReactDOM from "react-dom";
import Pagination from "../common/pagination";
import WebAPI from "../service/api/base";
import History from "./withdrawal.history";

/* eslint-disable */
class Panel extends Component {
	constructor(props) {
		super(props);
		this.startDateRef = createRef();
		this.endDateRef = createRef();
		this.state = {
			StartDate: moment().add(-30, "day").format("YYYY-MM-DD"),
			EndDate: moment().format("YYYY-MM-DD"),
			GetData: false
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
	GetHistory = pageNumber => {
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
			.post("/api/Payment/GetWithdrawalInfoHistory", data)
			.then(
				response => {
					let result = response.data;
					if (result.errorCode == 0) {
						this.setState({
							PageNumber: result.pageNumber,
							TotalPages: result.totalPages,
							History: result.data,
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
	render() {
		return (
			<div className="content">
				<p className="txt-note text-bold">{i18n["lbl_Date"]}:</p>
				<div className="form">
					<div className="calendar-group">
						<div className="form-input">
							<span className="label">{i18n["lbl_From"]}: </span>
							<div className="input-field">
								<input
									className="input-control"
									type="text"
									ref={this.startDateRef}
									defaultValue={this.state.StartDate}
								/>
								<span className="select-day">
									<i className="icon icon-calendar"></i>
								</span>
							</div>
						</div>
						<div className="form-input">
							<span className="label">{i18n["lbl_To"]}: </span>
							<div className="input-field">
								<input
									className="input-control"
									type="text"
									ref={this.endDateRef}
									defaultValue={this.state.EndDate}
								/>
								<span className="select-day">
									<i className="icon icon-calendar"></i>
								</span>
							</div>
						</div>
						<button
							className="btn btn-primary"
							onClick={this.GetHistory.bind(this, 1)}>
							{i18n["lbl_Submit"]}
						</button>
					</div>
				</div>
				{this.state.GetData ? (
					<History Data={this.state.History} />
				) : null}
				<Pagination
					PageNumber={this.state.PageNumber}
					TotalPages={this.state.TotalPages}
					onPaging={this.GetHistory}
				/>
			</div>
		);
	}
}

/* eslint-enable */
export default Panel;

if (process.env.NODE_ENV === "production") {
	ReactDOM.render(<Panel />, document.getElementById("content"));
}

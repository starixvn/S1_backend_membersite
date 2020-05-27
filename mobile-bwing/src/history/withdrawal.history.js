import React, { Component } from "react";
import { number_format } from "../common/common";

/* eslint-disable */
class HistoryBody extends Component {
	render() {
		let data = this.props.Data;
		let rows = data.map(
			function (item) {
				let dateMonent = moment(item.createTime);
				let statusGroup = null;

				if (
					item.status == 5 ||
					item.status == 6 ||
					item.status == 7 ||
					item.status == 99
				) {
					statusGroup = (
						<span>
							{i18n["lbl_WithdrawalStatus" + item.status]}
						</span>
					);
				} else {
					statusGroup = <span>{i18n.lbl_WithdrawalStatus4}</span>;
				}

				return (
					<div className="group" key={item.transId}>
						<div className="toggle option-header">
							<span className="text">
								{dateMonent.format("l")} (
								{dateMonent.format("Z")})
							</span>
							<i className="icon icon-arrow-down"></i>
						</div>
						<div className="history">
							<div className="history-item">
								<span>{i18n["lbl_Time"]}</span>
								<span>{dateMonent.format("hh:mm:ss A")}</span>
							</div>
							<div className="history-item">
								<span>{i18n["lbl_ID"]}</span>
								<span>{item.transId}</span>
							</div>
							<div className="history-item">
								<span>{i18n["lbl_Amount"]}</span>
								<span>
									{number_format(item.amount.toFixed(2))}
								</span>
							</div>
							<div className="history-item">
								<span>{i18n["lbl_Status"]}</span>
								{statusGroup}
							</div>
						</div>
					</div>
				);
			}.bind(this)
		);

		let dateStr = moment(data[0].createTime).format("YYYY/MM/DD");
		return (
			<div className="tbody" data-date={dateStr} data-open="true">
				{rows}
			</div>
		);
	}
}

export default class WithdrawalHistory extends Component {
	constructor(props) {
		super(props);
	}
	render() {
		let data = this.props.Data;
		let bodyData = null;
		if (data != null) {
			let dataContainer = [];
			for (let i = 0; i < data.length; i++) {
				let row = data[i];
				let mDate = moment(row.createTime);
				let sDate = mDate.format("YYYYMMDD");
				if (
					dataContainer.length == 0 ||
					dataContainer[dataContainer.length - 1].Date != sDate
				) {
					dataContainer.push({ Date: sDate, Data: [row] });
				} else {
					dataContainer[dataContainer.length - 1].Data.push(row);
				}
			}

			bodyData = dataContainer.map(
				function (item, index) {
					return (
						<HistoryBody
							key={index}
							Data={item.Data}
							onShowBankModal={this.props.onShowBankModal}
						/>
					);
				}.bind(this)
			);
		}

		return data != null && data.length > 0 ? (
			<div className="history-section">
				<div className="option-header">
					<span className="text-bold">
						{i18n.lbl_TransactionHistory}
					</span>
				</div>
				{bodyData}
			</div>
		) : (
			<div className="no-data">
				<span className="text">{i18n.msg_NoData}</span>
			</div>
		);
	}
}
/* eslint-enable */

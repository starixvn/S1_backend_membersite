import React, { Component } from "react";
import { number_format } from "../../common/common";

const DepositStatus = {
	Pending: 1,
	Paid: 2,
	Approved: 3,
	Rejected: 4,
	Canceled: 5,
	Expired: 6,
	OnHold: 7
};

export default class RecentHistoryBody extends React.Component {
	render() {
		let data = this.props.Data;

		let rows = data.map(
			function (item) {
				let dateMonent = moment(item.createTime);
				let btnGroup = null;
				let statusGroup = null;

				if (
					item.depositType == 3 &&
					(item.status == DepositStatus.Pending ||
						item.status == DepositStatus.OnHold)
				) {
					// btnGroup = (
					// 	<div className="btn-group">
					// 		{item.status == DepositStatus.Pending ? (
					// 			<button
					// 				className="btn btn-default"
					// 				type="button"
					// 				onClick={this.onCancel.bind(
					// 					this,
					// 					item.transId
					// 				)}>
					// 				<span className="text">
					// 					{i18n.lbl_Cancel}
					// 				</span>
					// 			</button>
					// 		) : null}
					// 		<button
					// 			className="btn btn-secondary"
					// 			type="button"
					// 			onClick={this.onShowBankModal.bind(this, {
					// 				...item.depositRep,
					// 				...item.bankInfo,
					// 				amount: item.amount,
					// 				postScript: item.postScript,
					// 				transId: item.transId,
					// 				refId: item.refId,
					// 				depositTime: item.depositTime,
					// 				picFileName: item.picFileName
					// 			})}>
					// 			<span className="text">
					// 				{i18n.lbl_SubmitProof}
					// 			</span>
					// 		</button>
					// 	</div>
					// );
				}

				if (
					item.status == DepositStatus.Pending ||
					item.status == DepositStatus.Approved ||
					item.status == DepositStatus.Rejected ||
					item.status == DepositStatus.Canceled
				) {
					statusGroup = (
						<span>{i18n["lbl_DepositStatus" + item.status]}</span>
					);
				} else {
					statusGroup = <span>{i18n.lbl_DepositStatus8}</span>;
				}

				return (
					<tr key={`dp-history-${item.transId}`}>
						<td>
							{dateMonent.format("l")} ({dateMonent.format("Z")})
						</td>
						<td>{dateMonent.format("hh:mm:ss A")}</td>
						<td>{item.transId}</td>
						<td>{i18n["lbl_DepositType" + item.depositType]}</td>
						<td>{number_format(item.amount.toFixed(2))}</td>
						<td>
							{statusGroup}
							{btnGroup}
						</td>
					</tr>
				);
			}.bind(this)
		);

		let dateStr = moment(data[0].createTime).format("YYYY/MM/DD");
		return (
			<table>
				<thead>
					<tr>
						<th>{i18n.lbl_Date}</th>
						<th>{i18n.lbl_Time}</th>
						<th>{i18n.lbl_ID}</th>
						<th>{i18n.lbl_Method}</th>
						<th>{i18n.lbl_Amount}</th>
						<th>{i18n.lbl_Status}</th>
					</tr>
				</thead>
				<tbody>{rows}</tbody>
			</table>
		);
	}
}

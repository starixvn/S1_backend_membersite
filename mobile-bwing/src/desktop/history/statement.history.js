import React from "react";
import { number_format } from "../../common/common";

/* eslint-disable */

const ChangeAmountFormat = (amount, direction) => {
	return `${direction == 1 ? "+" : "-"}${number_format(amount.toFixed(2))}`;
};

const AmountFormat = amount => {
	return number_format(amount.toFixed(2));
};

const DateFormat = time => {
	let dateMoment = moment(time);
	return `${dateMoment.format("l")} (${dateMoment.format("Z")})`;
};

const TimeFormat = time => {
	return moment(time).format("hh:mm:ss A");
};

const columnDefs = [
	{
		datacolumn: "createTime",
		displayText: i18n.lbl_Date,
		className: "td-date",
		format: { func: DateFormat }
	},
	{
		datacolumn: "createTime",
		displayText: i18n.lbl_Time,
		className: "td-time",
		format: { func: TimeFormat }
	},
	{
		datacolumn: "typeName",
		displayText: i18n.lbl_TypeName,
		className: "td-method"
	},
	{
		datacolumn: "changeAmount",
		displayText: i18n.lbl_ChangeAmount,
		className: "td-amount",
		format: { func: ChangeAmountFormat, bindDatacolumn: "direction" }
	},
	{
		datacolumn: "beforeAmount",
		displayText: i18n.lbl_BeforeAmount,
		className: "td-amount",
		format: { func: AmountFormat }
	},
	{
		datacolumn: "afterAmount",
		displayText: i18n.lbl_AfterAmount,
		className: "td-amount",
		format: { func: AmountFormat }
	},
	{
		datacolumn: "refTransId",
		displayText: i18n.lbl_RefTransId,
		className: "td-id"
	}
];

const Td = props => {
	return <td className={props.className}>{props.displayText}</td>;
};

const TBody = props => {
	let dataContainer = [];
	props.Datas.forEach(row => {
		let mDate = moment(row.createTime);
		let sDate = mDate.format("YYYYMMDD");
		if (
			dataContainer.length == 0 ||
			!dataContainer.find(x => x.Date === sDate)
		) {
			dataContainer.push({ Date: sDate, Data: [row] });
		} else {
			dataContainer[dataContainer.length - 1].Data.push(row);
		}
	});

	return dataContainer.map((section, sId) => {
		let child = section.Data.map((data, dId) => {
			let tdResult = columnDefs.map((col, cIdx) => {
				return (
					<Td
						key={`result-${sId}-${dId}-${cIdx}`}
						className={col.className}
						dataTitle={col.displayText}
						firstElem={cIdx === 0}
						displayText={
							col.format
								? col.format.func(
										data[col.datacolumn],
										col.format.bindDatacolumn
											? data[col.format.bindDatacolumn]
											: null
								  )
								: data[col.datacolumn]
						}
					/>
				);
			});
			return <tr key={`result-${section.Date}-${dId}`}>{tdResult}</tr>;
		});

		return child;
	});
};

const ResultTable = props => {
	return props.Datas && props.Datas.length > 0 ? (
		<div className="table-content">
			<div className="option-header">
				<span className="text-bold">{i18n.lbl_TransactionHistory}</span>
			</div>
			<table>
				<thead>
					<tr>
						<th className="td-date">{i18n.lbl_Date}</th>
						<th className="td-time">{i18n.lbl_Time}</th>
						<th className="td-method">{i18n.lbl_TypeName}</th>
						<th className="td-amount">{i18n.lbl_ChangeAmount}</th>
						<th className="td-amount">{i18n.lbl_BeforeAmount}</th>
						<th className="td-amount">{i18n.lbl_AfterAmount}</th>
						<th className="td-id">{i18n.lbl_RefTransId}</th>
					</tr>
				</thead>
				<tbody>
					<TBody Datas={props.Datas} />
				</tbody>
			</table>
		</div>
	) : (
		<div className="no-data">
			<span className="text">{i18n.msg_NoData}</span>
		</div>
	);
};

export default ResultTable;
/* eslint-enable */

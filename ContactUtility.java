package com.xxx.contactupate;

import java.io.ByteArrayOutputStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.StringReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.URLConnection;

import android.annotation.SuppressLint;
import android.database.Cursor;
import android.content.ContentProviderOperation;
import android.content.ContentProviderOperation.Builder;
import android.content.Context;
import android.content.OperationApplicationException;
import android.os.RemoteException;
import android.provider.ContactsContract;
import android.provider.ContactsContract.CommonDataKinds.Phone;
import android.provider.ContactsContract.Data;
import android.util.Log;

import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;

import org.w3c.dom.Document;
import org.w3c.dom.NodeList;
import org.xml.sax.InputSource;
import org.xml.sax.SAXException;

/**
 * This class provides methods for connecting to web service to get data for a 
 * given email id and to update phone number in android devices phone book
 * The remote location of web service is defined as urlString and that has to be changed. 
 * ASSUMPTION - I assumed a conventional SOAP web service that exchange data in XML format.
 * New RESTful services use JSON format to exchange data and for testing in local machine,
 * I found setting up SOAP based web service easier.  However, the code can be easily change to 
 * JSON based architecture  by simply changing the CONTENT_TYPE  to "application/json" and making
 * the input json object (containing emailid). 
 * 
 * In addition to email, we would want to send  some secured API Key or HASHKEY attached to user
 * along with email.  I have not implemented the security  mechanism yet.
 * 
 * @author benktesh
 *
 */

public class ContactUtility {
	private static final String TAG ="ContactUtility";
	private static final String urlString = "http://XXX/services/API?wsdl";
    
	private Context context;
	
	/**
	 * This method handles the request from the client.
	 * For testing, hard-coded value pairs are passed.
	 * @param context
	 * @param email
	 * @return
	 */
	public boolean processContactUtilityRequest(Context context, String email){
	   this.context = context;
	   boolean updateStatus=false;
	   try {
		   HashMap<String, String> webContact = getContactFromWeb(email);
		   String phone = webContact.get("TELEPHONE");
		   updateStatus = updateTelephone(email, phone, Phone.TYPE_MOBILE);
	   } 
	   catch (IOException e) {
		// TODO Auto-generated catch block
		e.printStackTrace();
	   }
	   
	   return updateStatus;
	}
   
   /**
    * This method calls the Web Service. The Method also assumes that the web service
    * is running traditional SOAP service and exchanges data in xml format. I used
    * this xml format as I found simple web service in soap easy to create for testing.
    * 
    * The method can be modified with minimal effort to so that the data can be exchanged
    * directly in JSON format.
    *   
    * @param - email : an email for the contact
    * @return -HashMap : key:valu pair of contract attribute.
    */
	 private HashMap<String,String> getContactFromWeb(String email) throws IOException {
		 HashMap<String, String> hm = new HashMap<String,String>();
	    
		 Log.e(TAG, "getContact Method");
		 String responseString = "";
		 String outputString = "";
		 
			
		 URL url = new URL(urlString);
		 URLConnection connection = url.openConnection();
		 HttpURLConnection httpConn = (HttpURLConnection)connection;
		 ByteArrayOutputStream bout = new ByteArrayOutputStream();
		
		 // The xml based web call can be replaced with JSON based call. See the comments at the
		 // top of the class
		 String xmlInput = "<soapenv:Envelope" +
				 " xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" "+
				 " xmlns:q0=\"http://gwabbit\"" +
				 " xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\""+ 
				 " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"+
				 "<soapenv:Header>" +
				 "</soapenv:Header>" +
				 "<soapenv:Body>" +
				 "<getContact>" +
				 "<email>"+email+"</email>" +
				 "</getContact>" +
				 "</soapenv:Body> " +
				 "</soapenv:Envelope>";	
				
		byte[] buffer = new byte[xmlInput.length()];
		buffer = xmlInput.getBytes();
		bout.write(buffer);
		byte[] b = bout.toByteArray();
		String SOAPAction = urlString; 
		
		// Set the appropriate HTTP parameters.
		httpConn.setRequestProperty("Content-Length",
		String.valueOf(b.length));
		httpConn.setRequestProperty("Content-Type", "text/xml; charset=utf-8");
		httpConn.setRequestProperty("SOAPAction", SOAPAction);
		httpConn.setRequestMethod("POST");
		httpConn.setDoOutput(true);
		httpConn.setDoInput(true);
		httpConn.connect();
			
		OutputStream out = httpConn.getOutputStream();
		out.write(b);
		out.close();
			
		//Read the response.
		InputStreamReader isr = new InputStreamReader(httpConn.getInputStream());
		BufferedReader in = new BufferedReader(isr);
			
		httpConn.disconnect();
			 
		//Write the SOAP message response to a String.
		while ((responseString = in.readLine()) != null) {
			outputString = outputString + responseString;
		}
		//Node item contains kew:value pairs.
		Document document = parseXmlFile(outputString);
		NodeList nodeLst = document.getElementsByTagName("item");
		
		for (int i = 0; i < nodeLst.getLength(); i++) {
			String nodeName = nodeLst.item(i).getChildNodes().item(0).getTextContent();
			String nodeContent = nodeLst.item(i).getChildNodes().item(1).getTextContent();
			//Ignore the 'unique dataid' i.e., ID column in the database (if any) 
			if (!nodeName.equals("ID"))
				hm.put(nodeName, nodeContent);
		}
		Log.e(TAG, "Returned data from the Web Service " + hm.toString());
		
		//}
		
	    return hm;
	 }
	
	 /**
	 * This method takes an email id, and returns the handle (as raw_contact_id) from the contact database.
	 * Raw contact id is specific to one contact that can be used to dataset from multiple table as this 
	 * id is unique to that particular contact. In order for this to work, an email id has to be unique instance 
	 * for a given situation. 
	 * @param - emailId
	 * @return
	 */
	 @SuppressLint("InlinedApi")
	 private String getContactReference(String emailId) {
		 String contactId= null;
		 String[] PROJECTION = new String[] { 
				 ContactsContract.Data.RAW_CONTACT_ID,
				 ContactsContract.CommonDataKinds.Email.ADDRESS,
		 };
	       
		 String filter = ContactsContract.CommonDataKinds.Email.ADDRESS + " = '"+ emailId+ "'";  
		 Cursor cursorPhone = context.getContentResolver().query(ContactsContract.Data.CONTENT_URI,  PROJECTION,  filter,  null,  null);
		 if (cursorPhone.moveToFirst())
			 contactId = cursorPhone.getString(0); 		   
		 cursorPhone.close();
		 
		 Log.i(TAG, "Contact ID is " + contactId);
		 return contactId;
	}

	/**
	 * This method update the phone number of the contact from the provided contact reference.
	 * The reference is raw_conteact id - a base id for each contact. 
	 * The assumption is that the the passed reference is for mobile phone. For other phone types, 
	 * Change the Phone.Type_Mobile parameter.
	 * @param emailId
	 * @param newTelephone
	 * @param type - type of phone number (i.e. mobile, work etc)
	 */
	private boolean updateTelephone(String emailId, String newTelephone, int type) {
			String contactId = getContactReference(emailId);
			boolean result = false;
			
			if (contactId!=null)
			{
				ArrayList<ContentProviderOperation> ops = new ArrayList<ContentProviderOperation>();
				String selection = ContactsContract.Data.RAW_CONTACT_ID + "=? AND " + Phone.MIMETYPE + "=? AND " + Phone.TYPE + "=?";
	    	
				Builder updateOp = ContentProviderOperation.newUpdate(ContactsContract.Data.CONTENT_URI).withSelection(selection,
	   				new String[] { contactId + "", Phone.CONTENT_ITEM_TYPE, String.valueOf(type) + ""}).withValue(Phone.NUMBER, newTelephone);
				ops.add(updateOp.build());   
	    	
				try {
					context.getContentResolver().applyBatch(ContactsContract.AUTHORITY, ops);
				} 
				catch (RemoteException e) {
	   					//e.printStackTrace();
	   					Log.e(TAG,e.getMessage());
				} 
				catch (OperationApplicationException e) {
	   					// TODO Auto-generated catch block
	   					//e.printStackTrace();
	   					Log.e(TAG,e.getMessage());
				}
				catch (Exception e) {
				// TODO Auto-generated catch block
					//e.printStackTrace();
					Log.e(TAG,e.getMessage());
				}
	   			result = true; 
				Log.i(TAG, "Successful: Updated Phone for contactid"+ contactId + " With telephone "+ newTelephone);
	   		
			}
			else {
				//Probably we will insert the contact detail here.
				result = false;
				Log.i(TAG, "Unsuccessful: No Update Made as contact does not exist for the email id. Suggest adding a contact first" + emailId);
			}
			return result;
	}
   
   @SuppressWarnings("unused")
   private void updateContact(String contactId,String newTelephone) {
	   	ArrayList<ContentProviderOperation> ops = new ArrayList<ContentProviderOperation>();
    	String selection = ContactsContract.Data.CONTACT_ID + "=? AND " + Phone.MIMETYPE + "=? AND " + Phone.TYPE + "=?";
    	
    	Builder updateOp = ContentProviderOperation.newUpdate(Data.CONTENT_URI).withSelection(selection,
   				new String[] { contactId + "", Phone.CONTENT_ITEM_TYPE, String.valueOf(Phone.TYPE_WORK) + "" }).withValue(Phone.DATA, newTelephone);
   		ops.add(updateOp.build());   
    	
   	    try {
   	    	context.getContentResolver().applyBatch(ContactsContract.AUTHORITY, ops);
   	    } 
   	    catch (RemoteException e) {
   	    	// TODO Auto-generated catch block
   	    	//e.printStackTrace();
   	    	Log.e(TAG,e.getMessage().toString());
   	    } 
   	    catch (OperationApplicationException e) {
   	    	// TODO Auto-generated catch block
   	    	//e.printStackTrace();
   	    	Log.e(TAG,e.getMessage().toString());
   	    }
   	    catch (Exception e) {
   	    	// TODO Auto-generated catch block
			//e.printStackTrace();
   	    	Log.e(TAG,e.getMessage().toString());
   	    }		
   	    Log.i(TAG, "Updated Contactfor contactid"+ contactId + " With telephone "+ newTelephone);
   }
   
   
   /**
	  * Help method to parse the XML string into XML document. 
	  * To be used when web service returns data in xml format 
	  * @param String in : 
	  * @return Document : xmlDocument
	  */
   private Document parseXmlFile(String inputXML) {
	   try {
		   DocumentBuilderFactory dbf = DocumentBuilderFactory.newInstance();
		   DocumentBuilder db = dbf.newDocumentBuilder();
		   InputSource is = new InputSource(new StringReader(inputXML));
		   return db.parse(is);
	   } 
	   catch (ParserConfigurationException e) {
			 throw new RuntimeException(e);
	   } 
		 catch (SAXException e) {
			 throw new RuntimeException(e);
		 } 	
		 catch (IOException e) {
			 throw new RuntimeException(e);
		 }
	 }
}
